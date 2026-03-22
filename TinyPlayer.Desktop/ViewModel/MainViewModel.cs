using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Windows;
using TinyPlayer.Core;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TinyPlayer.Desktop.ViewModel;

public partial class MainViewModel : BaseViewModel
{
    private VideoPlayerCore? _core;
    private bool _coreUpdating;
    private IntPtr _hwnd;

    [ObservableProperty] private long _position;
    [ObservableProperty] private long _duration;
    [ObservableProperty] private string _timeText = "00:00 / 00:00";
    [ObservableProperty] private string _streamInfo = string.Empty;

    partial void OnPositionChanged(long value)
    {
        if (_coreUpdating) return;
        _core?.SeekTo((int)value);
        TimeText = FormatTime(value, _duration);
    }

    // Public API

    /// <summary>Called from the code-behind once after the Loaded event.</summary>
    public void SetVideoHandle(IntPtr hwnd) => _hwnd = hwnd;

    [RelayCommand(CanExecute = nameof(HasCore))]
    private void Play() => _core?.Play();

    [RelayCommand(CanExecute = nameof(HasCore))]
    private void Pause() => _core?.Pause();

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

        _core.StateChanged += _ =>
            Application.Current?.Dispatcher.Invoke(() =>
            {
                PlayCommand.NotifyCanExecuteChanged();
                PauseCommand.NotifyCanExecuteChanged();
                StopCommand.NotifyCanExecuteChanged();
            });
    }

    // Helpers
    private void SetPosition(long s) 
    { 
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

    private void DisposeCore()
    {
        _core?.Dispose();
        _core = null;
        SetPosition(0);
        SetDuration(0);
    }

}