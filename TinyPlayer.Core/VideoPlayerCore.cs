using GLib;
using Gst;
using Gst.Video;
using System.Diagnostics;
using System.Text;
using TinyPlayer.Core.Enums;
using TinyPlayer.Core.Models;
using Constants = Gst.Constants;
using TagList = Gst.TagList;
using Thread = System.Threading.Thread;

namespace TinyPlayer.Core;

public class StreamsAnalysedEventArgs(MetadataModel metadata, string info) : EventArgs
{
    public MetadataModel Metadata { get; } = metadata;
    public string StreamInfo { get; } = info;
}

public sealed class VideoPlayerCore : IDisposable
{
    private const int SeekDelayMs = 250; // 33 = 30fps
    private Element? _playbin;
    private MainLoop? _mainLoop;
    private Thread? _mainGlibThread;
    private uint _refreshUiHandle;
    private long _duration = -1;
    private bool _disposed;
    private readonly nint _hwnd;

    public event Action<long, long>? PositionChanged;

    public event EventHandler<StreamsAnalysedEventArgs>? StreamsAnalysed;

    public event Action<string>? ErrorOccurred;

    public event Action? EndOfStream;

    public event Action<State>? StateChanged;

    public VideoPlayerCore(string uri, nint hwnd)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentNullException(nameof(uri));
        }

        _hwnd = hwnd;

        Gst.Application.Init();
        GtkSharp.GstreamerSharp.ObjectManager.Initialize();

        InitPipeline(uri);
    }

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
            Trace.WriteLine("[Audio] directsoundsink set");
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
            var msg = (Gst.Message)args.Args[0];
            if (msg.Type != MessageType.Element) return;
            if (msg.Structure?.Name != "prepare-window-handle") return;

            var overlay = new VideoOverlayAdapter(msg.Src.Handle);
            overlay.WindowHandle = _hwnd;
            overlay.HandleEvents(true);
            Trace.WriteLine("[VIDEO] HWND attached");
        };

        _playbin.Connect("video-tags-changed", TagsCb);
        _playbin.Connect("audio-tags-changed", TagsCb);
        _playbin.Connect("text-tags-changed", TagsCb);

        _playbin.SetProperty("flags", new GLib.Value((uint)AvFlagsType.EnableAllFlags));

        // Waiting for the actual Paused evenе only then is the duration known
        _playbin.SetState(State.Paused);
        _playbin.GetState(out _, out _, Constants.SECOND * 5);
        _playbin.SetState(State.Playing);

        _refreshUiHandle = GLib.Timeout.Add(SeekDelayMs, OnRefreshTimer);
    }

    public void Play() => _playbin?.SetState(State.Playing);

    public void Pause() => _playbin?.SetState(State.Paused);

    public void Stop() => _playbin?.SetState(State.Ready);

    public void SeekTo(int seconds)
        => _playbin?.SeekSimple(
            Format.Time,
            SeekFlags.Flush | SeekFlags.Accurate,
            (long)seconds * Constants.SECOND);

    public void SetAudioTrack(int index)
    {
        if (_playbin != null)
        {
            _playbin["current-audio"] = index;
        }
    }

    public void SetSubtitleTrack(int index)
    {
        if (_playbin != null)
        {
            _playbin["current-text"] = index;
        }
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
        _playbin["flags"] = enabled ? flags | (uint)AvFlagsType.Audio
                                    : flags & ~(uint)AvFlagsType.Audio;
    }

    public void SetVolume(double value)
    {
        if (_playbin == null)
        {
            return;
        }

        _playbin["volume"] = value;
        Trace.WriteLine($"[Volume] playbin: {value}");
    }

    public void ApplyFlags(AvFlagsType flags)
        => _playbin?.SetProperty("flags", new GLib.Value((uint)flags));

    private void OnElementMessage(object o, GLib.SignalArgs args)
    {
        var msg = (Gst.Message)args.Args[0];

        var s = msg.Structure;
        if (s == null)
            return;

        if (s.Name != "d3d11-present")
            return;

        try
        {
            var handle = (IntPtr)s.GetValue("shared-handle");
            int width = (int)s.GetValue("width");
            int height = (int)s.GetValue("height");

            Trace.WriteLine($"[D3D11] frame {width}x{height}, handle={handle}");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[D3D11 ERROR] {ex}");
        }
    }

    private bool OnRefreshTimer()
    {
        if (_playbin == null)
        {
            return false;
        }

        _playbin.GetState(out State state, out _, 0);
        if (state != State.Playing && state != State.Paused)
        {
            return true;
        }

        if (_duration < 0)
        {
            _playbin.QueryDuration(Format.Time, out _duration);
        }

        if (_playbin.QueryPosition(Format.Time, out long current))
        {
            PositionChanged?.Invoke(current / Constants.SECOND,
                                    _duration > 0 ? _duration / Constants.SECOND : 0);
        }

        return true;
    }

    private void ErrorCb(object o, SignalArgs args)
    {
        var msg = (Message)args.Args[0];
        msg.ParseError(out GException err, out string debug);
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
        msg.ParseStateChanged(out _, out State newState, out _);

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
        var el = sender as Element;
        el?.PostMessage(Message.NewApplication(el, new Structure("tags-changed")));
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
            NumOfSubtitles = (int)_playbin["n-text"]
        };

        var sb = new StringBuilder();
        for (int? i = 0; i < metadata.NumOfVideoStreams; i++)
        {
            var tags = (TagList)_playbin.Emit("get-video-tags", i);
            if (tags == null)
            {
                continue;
            }

            sb.AppendLine($"Video stream {i}:");
            if (tags.GetString(Constants.TAG_VIDEO_CODEC, out string codec))
            {
                sb.AppendLine($"  codec: {codec}");
            }
            ((GLib.Opaque)tags).Dispose();
        }

        for (int? i = 0; i < metadata.NumOfAudioStreams; i++)
        {
            var tags = (TagList)_playbin.Emit("get-audio-tags", i);
            if (tags == null)
            {
                continue;
            }

            sb.AppendLine($"Audio stream {i}:");

            if (tags.GetString(Constants.TAG_AUDIO_CODEC, out string codec))
            {
                sb.AppendLine($"  codec: {codec}");
            }

            if (tags.GetString(Constants.TAG_LANGUAGE_CODE, out string lang))
            {
                sb.AppendLine($"  language: {lang}");
            }

            if (tags.GetUint(Constants.TAG_BITRATE, out uint rate))
            {
                sb.AppendLine($"  bitrate: {rate}");
            }
            ((GLib.Opaque)tags).Dispose();
        }

        for (int? i = 0; i < metadata.NumOfSubtitles; i++)
        {
            var tags = (TagList)_playbin.Emit("get-text-tags", i);
            if (tags == null)
            {
                continue;
            }
            sb.AppendLine($"Subtitle stream {i}:");

            if (tags.GetString(Constants.TAG_LANGUAGE_CODE, out string lang))
            {
                sb.AppendLine($"  language: {lang}");
            }
            ((GLib.Opaque)tags).Dispose();
        }
        StreamsAnalysed?.Invoke(this, new StreamsAnalysedEventArgs(metadata, sb.ToString()));
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
            GLib.Timeout.Remove(_refreshUiHandle); _refreshUiHandle = 0;
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
}