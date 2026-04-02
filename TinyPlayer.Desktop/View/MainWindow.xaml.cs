using System.Windows.Input;
using TinyPlayer.Desktop.ViewModel;
using Wpf.Ui.Controls;

namespace TinyPlayer.Desktop.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        public MainViewModel Vm { get; }

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            Vm = viewModel;
            DataContext = Vm;

            Loaded += (_, _) =>
            {
                Vm.SetVideoHandle(VideoSurface.Handle);
            };
        }

        private void OnSeekStarted(object sender, MouseButtonEventArgs e)
        {
            Vm.BeginSeek();
        }

        private void OnSeekCompleted(object sender, MouseButtonEventArgs e)
        {
            Vm.EndSeek();
        }

        private void Window_Closed(object sender, EventArgs e) => Vm.Dispose();
    }
}