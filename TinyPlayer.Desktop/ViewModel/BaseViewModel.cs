using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TinyPlayer.Core;
using TinyPlayer.Core.Models;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TinyPlayer.Desktop.ViewModel;

public abstract class BaseViewModel : ObservableObject, IDisposable
{
    protected VideoPlayerCore? Core;
    public ObservableCollection<StreamItem> AudioTracks { get; } = [];
    public ObservableCollection<StreamItem> SubtitleTracks { get; } = [];

    protected virtual void OpenFile(Action<string> loadUri)
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

        loadUri?.Invoke(new Uri(dlg.FileName).AbsoluteUri);
    }

    public void Dispose()
    {
        Core?.Dispose();
        Core = null;
    }
}