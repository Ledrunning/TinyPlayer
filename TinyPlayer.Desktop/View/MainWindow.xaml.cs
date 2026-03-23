using System.Windows;
using TinyPlayer.Desktop.ViewModel;
using Wpf.Ui.Controls;

namespace TinyPlayer.Desktop.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private MainViewModel Vm => (MainViewModel)DataContext;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            // Pass the HWND to the VM once it will store it and use it every time LoadUri is called
            Loaded += (_, _) => Vm.SetVideoHandle(VideoHost.Panel.Handle);
        }

        private void Window_Closed(object sender, EventArgs e) => Vm.Dispose();
    }
}
