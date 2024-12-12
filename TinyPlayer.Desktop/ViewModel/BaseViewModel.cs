using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace TinyPlayer.Desktop.ViewModel;

public abstract class BaseViewModel : ObservableObject, INavigationAware
{
    public void OnNavigatedTo()
    {
    }

    public void OnNavigatedFrom()
    {
    }
}