using System.Windows;

namespace MonoTools.VSExtension.Views
{
    /// <summary>
    ///     Interaktionslogik für ServersFound.xaml
    /// </summary>
    public partial class ServersFound : Window
    {
        public ServersFound()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            ViewModel = new ServersFoundViewModel();
            DataContext = ViewModel;
            Closing += (o, e) => ViewModel.StopLooking();
        }

        public ServersFoundViewModel ViewModel { get; set; }

        private void Select(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}