using CommunityToolkit.Mvvm.ComponentModel;

namespace TinyPlayer.Desktop.ViewModel;

public abstract class BaseViewModel : ObservableObject, IDisposable
{
    public void OnNavigatedTo()
    {
    }

    public void OnNavigatedFrom()
    {
    }

    public void Dispose()
    {
    }
}