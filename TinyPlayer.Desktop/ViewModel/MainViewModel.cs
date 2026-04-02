using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gst;
using System.Windows;
using TinyPlayer.Core;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Task = System.Threading.Tasks.Task;
using Uri = System.Uri;

namespace TinyPlayer.Desktop.ViewModel;

public partial class MainViewModel : BaseViewModel
{
    private VideoPlayerCore? _core;
    private bool _coreUpdating;
    private bool _isUserSeeking;
    private CancellationTokenSource? _seekCts;

    [ObservableProperty] private long _position;
    [ObservableProperty] private long _duration;
    [ObservableProperty] private string _timeText = "00:00 / 00:00";
    [ObservableProperty] private string _streamInfo = string.Empty;

    [ObservableProperty]
    private bool _isPlaying;

    [RelayCommand(CanExecute = nameof(HasCore))]
    private void TogglePlayPause()
    {
        if (_isPlaying)
        {
            _core?.Pause();
        }
        else
        {
            _core?.Play();
        }
    }

    [RelayCommand(CanExecute = nameof(HasCore))]
    private void Stop() { _core?.Stop(); SetPosition(0); }

    [RelayCommand]
    private void OpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open the video file",
            Filter = "Video|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm|All files|*.*"
        };

        if (dlg.ShowDialog() != true)
        {
            return;
        }

        LoadUri(new Uri(dlg.FileName).AbsoluteUri);
    }

    [ObservableProperty]
    private double _volume = 100;
    private nint _hwnd;

    [RelayCommand(CanExecute = nameof(HasCore))]
    private void ToggleMute()
    {
        Volume = Volume > 0 ? 0 : 100;
    }

    private bool HasCore() => _core != null;

    // Load
    private void LoadUri(string uri)
    {
        DisposeCore();

        _core = new VideoPlayerCore(uri, _hwnd);
        _core.PositionChanged += (cur, dur) =>
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                SetDuration(dur);
                SetPosition(cur);
                TimeText = FormatTime(cur, dur);
            });

        _core.StreamsAnalysed += (_, e) =>
            Application.Current?.Dispatcher.BeginInvoke(() =>
                StreamInfo = e.StreamInfo);

        //TODO : add custom message box!
        _core.ErrorOccurred += msg =>
            Application.Current?.Dispatcher.Invoke(() =>
                MessageBox.Show(msg, "Playback error",
                    MessageBoxButton.OK, MessageBoxImage.Error));

        _core.EndOfStream += () =>
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SetPosition(0);
                TimeText = FormatTime(0, Duration);
            });

        _core.StateChanged += state =>
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                IsPlaying = state == State.Playing;
                TogglePlayPauseCommand.NotifyCanExecuteChanged();
                StopCommand.NotifyCanExecuteChanged();
            });
    }

    partial void OnVolumeChanged(double value)
        => _core?.SetVolume(value / 100.0);

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
            await Task.Delay(200, cts.Token);

            if (!cts.IsCancellationRequested)
            {
                _core?.SeekTo((int)value);
            }
        }
        catch (TaskCanceledException)
        {
            // ignore
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
        _core?.SeekTo((int)Position);
    }

    // Helpers
    private void SetPosition(long s)
    {
        if (_isUserSeeking) return; // 💥 ключевой момент

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
        _core?.Dispose();
        _core = null;
        SetPosition(0);
        SetDuration(0);
    }

}