using System.Diagnostics;
using System.Text;
using GLib;
using Gst;
using Gst.Video;
using Microsoft.Extensions.Logging;
using Serilog;
using TinyPlayer.Core.Abstractions;
using TinyPlayer.Core.Enums;
using TinyPlayer.Core.Events;
using TinyPlayer.Core.Extensions;
using TinyPlayer.Core.Models;
using Application = Gst.Application;
using Constants = Gst.Constants;
using ObjectManager = GtkSharp.GstreamerSharp.ObjectManager;
using TagList = Gst.TagList;
using Thread = System.Threading.Thread;
using Timeout = GLib.Timeout;
using Value = GLib.Value;

namespace TinyPlayer.Core;

public sealed class VideoPlayerCore : IVideoPlayerCore, IDisposable
{
    private const int SeekDelayMs = 250; // 33 = 30fps
    private readonly nint _hwnd;
    private bool _disposed;
    private long _duration = -1;
    private Thread? _mainGlibThread;
    private MainLoop? _mainLoop;
    private Element? _playbin;
    private uint _refreshUiHandle;
    private readonly ILogger<VideoPlayerCore> _logger;

    public VideoPlayerCore(string uri, nint hwnd, ILogger<VideoPlayerCore> logger)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentNullException(nameof(uri));
        }

        _hwnd = hwnd;
        _logger = logger;

        Application.Init();
        ObjectManager.Initialize();

        InitPipeline(uri);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_refreshUiHandle != 0)
        {
            Timeout.Remove(_refreshUiHandle);
            _refreshUiHandle = 0;
        }

        if (_playbin != null)
        {
            _playbin.SetState(State.Ready);
            _playbin.SetState(State.Null);
            _playbin.Dispose();
            _playbin = null;
        }

        _mainLoop?.Quit();
        _mainGlibThread?.Join(TimeSpan.FromSeconds(3));
    }

    public event Action<long, long>? PositionChanged;

    public event EventHandler<StreamsAnalysedEventArgs>? StreamsAnalysed;

    public event Action<string>? ErrorOccurred;

    public event Action? EndOfStream;

    public event Action<State>? StateChanged;

    private void InitPipeline(string uri)
    {
        // MainLoop must have for bus events
        _mainLoop = new MainLoop();
        _mainGlibThread = new Thread(_mainLoop.Run) { IsBackground = true, Name = "GLibMain" };
        _mainGlibThread.Start();

        _playbin = ElementFactory.Make("playbin");
        _playbin["uri"] = uri;

        var audioSink = ElementFactory.Make("directsoundsink", "audio_sink");
        if (audioSink != null)
        {
            _playbin["audio-sink"] = audioSink;
            _logger.LogInformation("[Audio] directsoundsink set");
        }

        var sink = ElementFactory.Make("d3dvideosink", "video_sink");
        _playbin["video-sink"] = sink;

        var bus = _playbin.Bus;
        bus.AddSignalWatch();
        bus.EnableSyncMessageEmission();

        bus.Connect("message::error", ErrorCb);
        bus.Connect("message::eos", EosCb);
        bus.Connect("message::state-changed", StateChangedCb);
        bus.Connect("message::application", ApplicationCb);

        bus.SyncMessage += (o, args) =>
        {
            var msg = (Message)args.Args[0];
            if (msg.Type != MessageType.Element)
            {
                return;
            }

            if (msg.Structure?.Name != "prepare-window-handle")
            {
                return;
            }

            var overlay = new VideoOverlayAdapter(msg.Src.Handle);
            overlay.WindowHandle = _hwnd;
            overlay.HandleEvents(true);
            _logger.LogInformation("[VIDEO] HWND attached");
        };

        _playbin.Connect("video-tags-changed", TagsCb);
        _playbin.Connect("audio-tags-changed", TagsCb);
        _playbin.Connect("text-tags-changed", TagsCb);

        _playbin.SetProperty("flags", new Value((uint)AvFlagsType.EnableAllFlags));

        // Waiting for the actual Paused even only then is the duration known
        _playbin.SetState(State.Paused);
        _playbin.GetState(out _, out _, Constants.SECOND * 5);
        _playbin.SetState(State.Playing);

        _refreshUiHandle = Timeout.Add(SeekDelayMs, OnRefreshTimer);
    }

    public void Play()
    {
        _playbin?.SetState(State.Playing);
    }

    public void Pause()
    {
        _playbin?.SetState(State.Paused);
    }

    public void Stop()
    {
        _playbin?.SetState(State.Ready);
    }

    public void SeekTo(int seconds)
    {
        _playbin?.SeekSimple(
            Format.Time,
            SeekFlags.Flush | SeekFlags.Accurate,
            seconds * Constants.SECOND);
    }

    public void SetAudioTrack(int index)
    {
        _playbin?["current-audio"] = index;
    }

    public void SetSubtitleTrack(int index)
    {
        _playbin?["current-text"] = index;
    }

    public void SetSubtitlesEnabled(bool enabled)
    {
        if (_playbin == null)
        {
            return;
        }

        var flags = (uint)_playbin["flags"];

        if (enabled)
        {
            _playbin["flags"] = flags | (uint)AvFlagsType.SubText;
        }
        else
        {
            _playbin["flags"] = flags & ~(uint)AvFlagsType.SubText;
        }

        if (!enabled)
        {
            _playbin["current-text"] = -1;
        }
    }

    public void SetAudioEnabled(bool enabled)
    {
        if (_playbin == null)
        {
            return;
        }

        var flags = (uint)_playbin["flags"];
        _playbin["flags"] = enabled
            ? flags | (uint)AvFlagsType.Audio
            : flags & ~(uint)AvFlagsType.Audio;
    }

    public void SetVolume(double value)
    {
        if (_playbin == null)
        {
            return;
        }

        _playbin["volume"] = value;
        _logger.LogInformation("[Volume] playbin: {Value}", value);
    }

    private bool OnRefreshTimer()
    {
        if (_playbin == null)
        {
            return false;
        }

        _playbin.GetState(out var state, out _, 0);
        if (state != State.Playing && state != State.Paused)
        {
            return true;
        }

        if (_duration < 0)
        {
            _playbin.QueryDuration(Format.Time, out _duration);
        }

        if (_playbin.QueryPosition(Format.Time, out var current))
        {
            PositionChanged?.Invoke(current / Constants.SECOND,
                _duration > 0 ? _duration / Constants.SECOND : 0);
        }

        return true;
    }

    private void ErrorCb(object o, SignalArgs args)
    {
        var msg = (Message)args.Args[0];
        msg.ParseError(out var err, out var debug);
        _playbin?.SetState(State.Ready);
        ErrorOccurred?.Invoke($"Error from '{msg.Src.Name}': {err.Message}\nDebug: {debug ?? "(none)"}");
    }

    private void EosCb(object o, SignalArgs args)
    {
        _playbin?.SetState(State.Ready);
        EndOfStream?.Invoke();
    }

    private void StateChangedCb(object o, SignalArgs args)
    {
        var msg = (Message)args.Args[0];
        msg.ParseStateChanged(out _, out var newState, out _);

        if (msg.Src == _playbin)
        {
            StateChanged?.Invoke(newState);
        }
    }

    private void ApplicationCb(object o, SignalArgs args)
    {
        var msg = (Message)args.Args[0];

        if (msg.Structure?.Name == "tags-changed")
        {
            AnalyseStreams();
        }
    }

    private static void TagsCb(object sender, SignalArgs args)
    {
        var element = sender as Element;
        element?.PostMessage(Message.NewApplication(element, new Structure("tags-changed")));
    }

    private void AnalyseStreams()
    {
        if (_playbin == null)
        {
            return;
        }

        var metadata = new MetadataModel
        {
            NumOfVideoStreams = (int)_playbin["n-video"],
            NumOfAudioStreams = (int)_playbin["n-audio"],
            NumOfSubtitles = (int)_playbin["n-text"],

            // Current active tracks from playbin
            CurrentAudioIndex = (int)_playbin["current-audio"],
            CurrentSubtitleIndex = (int)_playbin["current-text"]
        };

        var sb = new StringBuilder();

        // Audio
        for (var i = 0; i < metadata.NumOfAudioStreams; i++)
        {
            var tags = (TagList)_playbin.Emit("get-audio-tags", i);

            string? lang = null;
            string? codec = null;
            uint rate = 0;

            if (tags != null)
            {
                tags.GetString(Constants.TAG_LANGUAGE_CODE, out lang);
                tags.GetString(Constants.TAG_AUDIO_CODEC, out codec);
                tags.GetUint(Constants.TAG_BITRATE, out rate);
                tags.Dispose();
            }

            // Title: Language (if available), otherwise codec, otherwise "Track N"
            var title = lang?.IsoToLanguageName(new MetadataLanguage
            {
                Codec = codec,
                Bitrate = rate,
                Index = i,
                Prefix = "Track"
            });

            metadata.AudioTracks.Add(new StreamItem { Index = i, Title = title });
            sb.AppendLine($"Audio {i}: {title}");
        }

        // Subtitles
        for (var i = 0; i < metadata.NumOfSubtitles; i++)
        {
            var tags = (TagList)_playbin.Emit("get-text-tags", i);

            string? lang = null;
            if (tags != null)
            {
                tags.GetString(Constants.TAG_LANGUAGE_CODE, out lang);
                tags.Dispose();
            }

            var title = !string.IsNullOrEmpty(lang) ? lang : $"Sub {i}";
            metadata.SubtitleTracks.Add(new StreamItem { Index = i, Title = title });
            sb.AppendLine($"Sub {i}: {title}");
        }
       
        StreamsAnalysed?.Invoke(this, new StreamsAnalysedEventArgs(metadata, sb.ToString()));
    }

    public void SetRenderSize(int w, int h)
    {
        if (_playbin == null)
        {
            return;
        }

        var overlay = ((Bin)_playbin)?.GetByInterface(VideoOverlayAdapter.GType);
        if (overlay == null)
        {
            return;
        }

        var adapter = new VideoOverlayAdapter(overlay.Handle);
        adapter.SetRenderRectangle(0, 0, w, h);
        adapter.Expose();
    }
}