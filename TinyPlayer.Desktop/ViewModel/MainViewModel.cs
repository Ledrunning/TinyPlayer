using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gst;
using TinyPlayer.Core;
using TinyPlayer.Core.Events;
using TinyPlayer.Core.Models;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Task = System.Threading.Tasks.Task;

namespace TinyPlayer.Desktop.ViewModel;

public partial class MainViewModel : BaseViewModel
{
    private const double MaxVolumeDelta = 100.0;
    private const int SeekDebounce = 100;
    private const int MaxVolumePercentage = 100;
    private readonly List<StreamItem> _allSubtitleTracks = [];
    private bool _coreUpdating;

    [ObservableProperty] private long _duration;

    private nint _hwnd;
    private bool _isInternalUpdate;

    [ObservableProperty] private bool _isMuted;

    [ObservableProperty] private bool _isPlaying;

    private bool _isUserSeeking;

    [ObservableProperty] private long _position;

    private CancellationTokenSource? _seekCts;

    [ObservableProperty] private StreamItem? _selectedAudioTrack;

    [ObservableProperty] private StreamItem? _selectedSubtitleTrack;

    [ObservableProperty] private string _streamInfo = string.Empty;

    private bool _streamsInitialized;

    [ObservableProperty] private bool _subtitlesEnabled;

    [ObservableProperty] private string _timeText = "00:00 / 00:00";

    [ObservableProperty] private double _volume = MaxVolumePercentage;

    [RelayCommand(CanExecute = nameof(HasCore))]
    private void TogglePlayPause()
    {
        if (IsPlaying)
        {
            Core?.Pause();
        }
        else
        {
            Core?.Play();
        }
    }

    [RelayCommand(CanExecute = nameof(HasCore))]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;

        Core?.SetVolume(IsMuted ? 0 : Math.Max(Volume / MaxVolumeDelta, 0.01));
    }

    [RelayCommand(CanExecute = nameof(HasCore))]
    private void Stop()
    {
        Core?.Stop();
        SetPosition(0);
    }

    [RelayCommand]
    private void OpenFile()
    {
        base.OpenFile(LoadUri);
    }

    private bool HasCore()
    {
        return Core != null;
    }

    // Load
    private void LoadUri(string uri)
    {
        DisposeCore();

        Core = new VideoPlayerCore(uri, _hwnd);
        Core.PositionChanged += (cur, dur) =>
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                SetDuration(dur);
                SetPosition(cur);
                TimeText = FormatTime(cur, dur);
            });

        Core?.StreamsAnalysed -= OnStreamsAnalysed;
        Core?.StreamsAnalysed += OnStreamsAnalysed;

        //TODO : add custom message box!
        Core?.ErrorOccurred += msg =>
            Application.Current?.Dispatcher.Invoke(() =>
                MessageBox.Show(msg, "Playback error",
                    MessageBoxButton.OK, MessageBoxImage.Error));

        Core?.EndOfStream += () =>
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SetPosition(0);
                TimeText = FormatTime(0, Duration);
            });

        Core?.StateChanged += state =>
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                IsPlaying = state == State.Playing;
                TogglePlayPauseCommand.NotifyCanExecuteChanged();
                StopCommand.NotifyCanExecuteChanged();
            });

        ToggleMuteCommand.NotifyCanExecuteChanged();
    }

    private void OnStreamsAnalysed(object? sender, StreamsAnalysedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _streamsInitialized = false; // Temporarily suspending the response to changes whilst we compile the lists
            _isInternalUpdate = true;

            try
            {
                AudioTracks.Clear();
                SubtitleTracks.Clear();
                _allSubtitleTracks.Clear();

                // Fill in with pre-defined names from the metadata
                foreach (var track in e.Metadata.AudioTracks)
                {
                    AudioTracks.Add(track);
                }

                foreach (var track in e.Metadata.SubtitleTracks)
                {
                    _allSubtitleTracks.Add(track);
                }

                // Restore the current audio track from the playbin
                SelectedAudioTrack = AudioTracks
                                         .FirstOrDefault(t => t.Index == e.Metadata.CurrentAudioIndex)
                                     ?? AudioTracks.FirstOrDefault();

                // Subtitles - fill in the list but do NOT switch tracks
                if (SubtitlesEnabled)
                {
                    foreach (var item in _allSubtitleTracks)
                    {
                        SubtitleTracks.Add(item);
                    }

                    // Retrieve the current subtitle from the playbin
                    // If CurrentSubtitleIndex == -1, there are no subtitles
                    SelectedSubtitleTrack = e.Metadata.CurrentSubtitleIndex >= 0
                        ? SubtitleTracks.FirstOrDefault(t => t.Index == e.Metadata.CurrentSubtitleIndex)
                        : SubtitleTracks.FirstOrDefault();
                }
                else
                {
                    SelectedSubtitleTrack = null;
                }
            }
            finally
            {
                _isInternalUpdate = false;
                _streamsInitialized = true;
            }
        });
    }

    private void ApplySubtitleFilter()
    {
        _isInternalUpdate = true;
        try
        {
            SubtitleTracks.Clear();

            if (!SubtitlesEnabled)
            {
                SelectedSubtitleTrack = null;
                return;
            }

            foreach (var item in _allSubtitleTracks)
            {
                SubtitleTracks.Add(item);
            }

            // Restore the selected track if it is in the list\
            // Do NOT automatically reset to the first track
            if (SelectedSubtitleTrack != null)
            {
                SelectedSubtitleTrack = SubtitleTracks
                    .FirstOrDefault(t => t.Index == SelectedSubtitleTrack.Index);
            }

            SelectedSubtitleTrack ??= SubtitleTracks.FirstOrDefault();
        }
        finally
        {
            _isInternalUpdate = false;
        }
    }

    partial void OnVolumeChanged(double value)
    {
        if (IsMuted)
        {
            return;
        }

        Core?.SetVolume(value / MaxVolumeDelta);
    }

    partial void OnPositionChanged(long value)
    {
        if (_coreUpdating)
        {
            return;
        }

        if (_isUserSeeking)
        {
            _ = DebouncedSeek(value);
        }
    }

    private async Task DebouncedSeek(long value)
    {
        _seekCts?.CancelAsync();

        var cts = new CancellationTokenSource();
        _seekCts = cts;

        try
        {
            await Task.Delay(SeekDebounce, cts.Token);

            if (!cts.IsCancellationRequested)
            {
                Core?.SeekTo((int)value);
            }
        }
        catch (TaskCanceledException)
        {
            // TODO: add log
        }
    }

    public void BeginSeek()
    {
        _isUserSeeking = true;
    }

    public void EndSeek()
    {
        _isUserSeeking = false;

        _seekCts?.Cancel();
        Core?.SeekTo((int)Position);
    }

    // Helpers
    private void SetPosition(long seekPosition)
    {
        if (_isUserSeeking)
        {
            return;
        }

        _coreUpdating = true;
        Position = seekPosition;
        _coreUpdating = false;
    }

    private void SetDuration(long duration)
    {
        _coreUpdating = true;
        Duration = duration;
        _coreUpdating = false;
    }

    private static string FormatTime(long cur, long dur)
    {
        return $@"{TimeSpan.FromSeconds(cur):mm\:ss} / {TimeSpan.FromSeconds(dur):mm\:ss}";
    }

    public void SetVideoHandle(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    partial void OnSelectedAudioTrackChanged(StreamItem? value)
    {
        if (_isInternalUpdate)
        {
            return;
        }

        if (!_streamsInitialized)
        {
            return;
        }

        if (value == null)
        {
            return;
        }

        Core?.SetAudioTrack(value.Index);
    }

    partial void OnSelectedSubtitleTrackChanged(StreamItem? value)
    {
        if (_isInternalUpdate)
        {
            return;
        }

        if (!_streamsInitialized)
        {
            return;
        }

        if (!SubtitlesEnabled)
        {
            return;
        }

        if (value == null)
        {
            return;
        }

        Core?.SetSubtitleTrack(value.Index);
    }

    partial void OnSubtitlesEnabledChanged(bool value)
    {
        if (!_streamsInitialized)
        {
            return;
        }

        Core?.SetSubtitlesEnabled(value);
        ApplySubtitleFilter();

        // If subtitles are enabled, activate the currently selected track
        if (value && SelectedSubtitleTrack != null)
        {
            Core?.SetSubtitleTrack(SelectedSubtitleTrack.Index);
        }
    }

    private void DisposeCore()
    {
        _streamsInitialized = false;
        Core?.StreamsAnalysed -= OnStreamsAnalysed;
        Dispose();
        SetPosition(0);
        SetDuration(0);
    }
}