using System.Windows;
using System.Windows.Forms;
using CTALookup.ViewModel;
using UserControl = System.Windows.Controls.UserControl;

namespace CTALookup
{
    /// <summary>
    /// Interaction logic for ContentView.xaml
    /// </summary>
    public partial class ContentView : UserControl
    {
        private ContentViewModel ViewModel {
            get {
                return this.DataContext as ContentViewModel;
            }
        }

        public ContentView()
        {
            InitializeComponent();
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            var dialog = new SaveFileDialog
                {
                    Filter = "CSV File|*.csv"
                };
            if (dialog.ShowDialog() == DialogResult.OK) {
                ViewModel.OutputFile = dialog.FileName;
            }

        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            var dialog = new FolderBrowserDialog
                {
                    ShowNewFolderButton = true
                };
            if (dialog.ShowDialog() == DialogResult.OK) {
                ViewModel.ImagesFolder = dialog.SelectedPath;
            }
        }

        private void DataGrid_Initialized(object sender, System.EventArgs e)
        {
            var grid = (System.Windows.Controls.DataGrid)sender;
            grid.LoadingRow += grid_LoadingRow;
        }

        void grid_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            var grid = (System.Windows.Controls.DataGrid)sender;
            //System.Windows.MessageBox.Show("qwe");
            if (grid.Columns[4].Width.DisplayValue > 300) grid.Columns[4].Width = 300;
            if (grid.Columns[5].Width.DisplayValue > 300) grid.Columns[5].Width = 300;
            if (grid.Columns[6].Width.DisplayValue > 300) grid.Columns[6].Width = 300;
        }
    }
}
