using System.Windows;
using CTALookup.GoogleMaps;
using CTALookup.ViewModel;

namespace CTALookup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e) {
            Close();
        }

        private void ContentView_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}