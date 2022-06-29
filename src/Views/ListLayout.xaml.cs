using COMPASS.Models;
using COMPASS.Resources.Controls;
using COMPASS.ViewModels;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;


namespace COMPASS.Views
{
    /// <summary>
    /// Interaction logic for ListLayout.xaml
    /// </summary>
    public partial class ListLayout : UserControl
    {
        public ListLayout()
        {
            InitializeComponent();
            LoadDataGridInfo();
        }

        public void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Codex toOpen = ((DataGridRow)sender).DataContext as Codex;
            CodexViewModel.OpenCodex(toOpen);
        }

        //Make sure selected Item is always in view
        private void FileView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid && e.AddedItems != null && e.AddedItems.Count > 0)
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
            ListLayoutGrid.ColumnInfo = JsonConvert.DeserializeObject<ObservableCollection<ColumnInfo>>(Properties.Settings.Default["DataGridCollumnInfo"].ToString());

        }

        private void FileView_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            LoadDataGridInfo();
        }
    }
}
