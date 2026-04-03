using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gst;
using System.Windows;
using TinyPlayer.Core;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Task = System.Threading.Tasks.Task;

namespace TinyPlayer.Desktop.ViewModel;

public partial class MainViewModel : BaseViewModel
{
    private bool _coreUpdating;
    private bool _isUserSeeking;
    private CancellationTokenSource? _seekCts;
    private const double MaxVolumeDelta = 100.0;
    private const int SeekDebounce = 100;
    private const int MaxVolumePercentage = 100;

    [ObservableProperty] private long _position;
    [ObservableProperty] private long _duration;
    [ObservableProperty] private string _timeText = "00:00 / 00:00";
    [ObservableProperty] private string _streamInfo = string.Empty;

    [ObservableProperty]
    private bool _isPlaying;
    [ObservableProperty]
    private bool _isMuted;

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
        Core?.SetVolume(IsMuted ? 0 : Volume / MaxVolumeDelta);
    }

    [RelayCommand(CanExecute = nameof(HasCore))]
    private void Stop() { Core?.Stop(); SetPosition(0); }

    [RelayCommand]
    private void OpenFile()
    {
        base.OpenFile(LoadUri);
    }

    [ObservableProperty]
    private double _volume = MaxVolumePercentage;
    private nint _hwnd;

    private bool HasCore() => Core != null;

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

        Core.StreamsAnalysed += (_, e) =>
            Application.Current?.Dispatcher.BeginInvoke(() =>
                StreamInfo = e.StreamInfo);

        //TODO : add custom message box!
        Core.ErrorOccurred += msg =>
            Application.Current?.Dispatcher.Invoke(() =>
                MessageBox.Show(msg, "Playback error",
                    MessageBoxButton.OK, MessageBoxImage.Error));

        Core.EndOfStream += () =>
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SetPosition(0);
                TimeText = FormatTime(0, Duration);
            });

        Core.StateChanged += state =>
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                IsPlaying = state == State.Playing;
                TogglePlayPauseCommand.NotifyCanExecuteChanged();
                StopCommand.NotifyCanExecuteChanged();
            });

        ToggleMuteCommand.NotifyCanExecuteChanged();
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
        if (_coreUpdating) return;

        if (_isUserSeeking)
        {
            DebouncedSeek(value);
        }
    }

    private async void DebouncedSeek(long value)
    {
        _seekCts?.Cancel();

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
    private void SetPosition(long s)
    {
        if (_isUserSeeking)
        {
            return;
        }

        _coreUpdating = true;
        Position = s;
        _coreUpdating = false;
    }

    private void SetDuration(long s)
    {
        _coreUpdating = true;
        Duration = s;
        _coreUpdating = false;
    }

    private static string FormatTime(long cur, long dur)
        => $"{TimeSpan.FromSeconds(cur):mm\\:ss} / {TimeSpan.FromSeconds(dur):mm\\:ss}";

    public void SetVideoHandle(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    private void DisposeCore()
    {
        Dispose();
        SetPosition(0);
        SetDuration(0);
    }

}