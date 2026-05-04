using System.Windows;
using System.Windows.Threading;
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

    [ObservableProperty] private Visibility _controlsVisibility = Visibility.Visible;

    private bool _coreUpdating;


    [ObservableProperty] private long _duration;

    private DispatcherTimer? _hideControlsTimer;

    private nint _hwnd;

    [ObservableProperty] private bool _isControlsVisible = true;

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

    [ObservableProperty] private Visibility _titleBarVisibility = Visibility.Visible;

    [ObservableProperty] private double _volume = MaxVolumePercentage;

    [ObservableProperty] private WindowState _windowState = WindowState.Normal;

    [ObservableProperty] private WindowStyle _windowStyle = WindowStyle.SingleBorderWindow;

    [RelayCommand]
    private void ToggleFullscreen()
    {
        var isFullscreen = WindowState == WindowState.Maximized;

        if (!isFullscreen)
        {
            // Switch to fullscreen — first set to None, then Maximised;
            // otherwise the taskbar won't be hidden
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            TitleBarVisibility = Visibility.Collapsed;
            ControlsVisibility = Visibility.Collapsed;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            TitleBarVisibility = Visibility.Visible;
            ControlsVisibility = Visibility.Visible;
        }
    }

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

    public void OnVideoClick()
    {
        IsControlsVisible = true;

        _hideControlsTimer?.Stop();
        _hideControlsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _hideControlsTimer.Tick += (_, _) =>
        {
            _hideControlsTimer.Stop();
            if (IsPlaying)
            {
                IsControlsVisible = false;
            }
        };
        _hideControlsTimer.Start();
    }

    private void OnStreamsAnalysed(object? sender, StreamsAnalysedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _streamsInitialized = false;
            _isInternalUpdate = true;

            try
            {
                AudioTracks.Clear();
                SubtitleTracks.Clear();
                _allSubtitleTracks.Clear();

                foreach (var track in e.Metadata.AudioTracks)
                {
                    AudioTracks.Add(track);
                }

                foreach (var track in e.Metadata.SubtitleTracks)
                {
                    _allSubtitleTracks.Add(track);
                }

                // Restore the audio track and apply it
                SelectedAudioTrack = AudioTracks
                                         .FirstOrDefault(t => t.Index == e.Metadata.CurrentAudioIndex)
                                     ?? AudioTracks.FirstOrDefault();

                if (SelectedAudioTrack != null)
                {
                    Core?.SetAudioTrack(SelectedAudioTrack.Index);
                }

                // Subtitles — populate the list but do not enable them
                // Enable only via the CC button using SubtitlesEnabled
                foreach (var item in _allSubtitleTracks)
                {
                    SubtitleTracks.Add(item);
                }

                SelectedSubtitleTrack = SubtitleTracks.FirstOrDefault();

                // Set the current state of SubtitlesEnabled
                Core?.SetSubtitlesEnabled(SubtitlesEnabled);

                if (SubtitlesEnabled && SelectedSubtitleTrack != null)
                {
                    Core?.SetSubtitleTrack(SelectedSubtitleTrack.Index);
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