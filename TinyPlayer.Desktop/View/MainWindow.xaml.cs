using System.Windows.Input;
using TinyPlayer.Desktop.ViewModel;
using Wpf.Ui.Controls;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TinyPlayer.Desktop.View;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        Vm = viewModel;
        DataContext = Vm;

        Loaded += (_, _) => { Vm.SetVideoHandle(VideoSurface.Handle); };
    }

    public MainViewModel Vm { get; }

    private void OnSeekStarted(object sender, MouseButtonEventArgs e)
    {
        Vm.BeginSeek();
    }

    private void OnSeekCompleted(object sender, MouseButtonEventArgs e)
    {
        Vm.EndSeek();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        Vm.Dispose();
    }

    private void VideoGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Vm.OnVideoClick();

        if (e.ClickCount == 2)
        {
            Vm.ToggleFullscreenCommand.Execute(null);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Vm.ToggleFullscreenCommand.Execute(null);
        }

        base.OnKeyDown(e);
    }

    private void VideoGrid_MouseMove(object sender, MouseEventArgs e)
    {
        Vm.OnVideoClick();
    }
}