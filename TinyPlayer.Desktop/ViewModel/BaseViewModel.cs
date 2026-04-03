using CommunityToolkit.Mvvm.ComponentModel;
using TinyPlayer.Core;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TinyPlayer.Desktop.ViewModel;

public abstract class BaseViewModel : ObservableObject, IDisposable
{
    protected VideoPlayerCore? Core;

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