using COMPASS.Resources.Controls;
using COMPASS.ViewModels;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;


namespace COMPASS.Views
{
    /// <summary>
    /// Interaction logic for FileListView.xaml
    /// </summary>
    public partial class FileListView : UserControl
    {
        public FileListView()
        {
            InitializeComponent();
            LoadDataGridInfo();
        }

        public void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ((FileBaseViewModel)DataContext).OpenFile();
        }

        //Make sure selected Item is always in view
        private void FileView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            if (dataGrid != null && e.AddedItems != null && e.AddedItems.Count > 0)
            {
                dataGrid.ScrollIntoView(e.AddedItems[0]);
            }
        }

        private void FileView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            LoadDataGridInfo();
        }

        private void LoadDataGridInfo()
        {
            FileView.ColumnInfo = JsonConvert.DeserializeObject<ObservableCollection<ColumnInfo>>(Properties.Settings.Default["DataGridCollumnInfo"].ToString());

        }

        private void FileView_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            LoadDataGridInfo();
        }
    }
}
