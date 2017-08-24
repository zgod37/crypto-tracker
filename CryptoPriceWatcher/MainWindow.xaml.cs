using System.Windows;

namespace CryptoPriceWatcher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            this.DataContext = new TickerContainerViewModel();
        }
    }
}
