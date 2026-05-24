using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TinyPlayer.Core;
using TinyPlayer.Core.Models;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TinyPlayer.Desktop.ViewModel;

public abstract class BaseViewModel : ObservableObject, IDisposable
{
    protected VideoPlayerCore? Core;
    public ObservableCollection<StreamItem> AudioTracks { get; } = [];
    public ObservableCollection<StreamItem> SubtitleTracks { get; } = [];

    public readonly List<StreamItem> AllSubtitleTracks = [];

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Core?.Dispose();
            Core = null;
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void OpenFile(Action<string> loadUri)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open the video file",
            Filter = "Video|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm|All files|*.*"
        };

        if (!dlg.ShowDialog().HasValue)
        {
            return;
        }

        loadUri.Invoke(new Uri(dlg.FileName).AbsoluteUri);
    }
}