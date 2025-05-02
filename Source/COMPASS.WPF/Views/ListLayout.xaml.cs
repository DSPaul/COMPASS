using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using COMPASS.Models;
using COMPASS.Resources.Controls;
using COMPASS.ViewModels;
using Newtonsoft.Json;


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
            if (sender is DataGridRow dataGridRow && dataGridRow.DataContext is Codex codex)
            {
                CodexViewModel.OpenCodex(codex);
            }
        }

        //Make sure selected Item is always in view
        private void FileView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid && e.AddedItems is { Count: > 0 })
            {
                try
                {
                    dataGrid.ScrollIntoView(e.AddedItems[0]);
                }
                catch
                {
                    //happens when first item is null
                }
            }
        }

        private void FileView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e) => LoadDataGridInfo();

        private void LoadDataGridInfo()
            => ListLayoutGrid.ColumnInfo = JsonConvert.DeserializeObject<ObservableCollection<ColumnInfo>>(Properties.Settings.Default["DataGridCollumnInfo"].ToString()!);

        private void FileView_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
            => LoadDataGridInfo();

        private void ListLayoutGrid_PreviewKeyDown(object sender, KeyEventArgs e) => CodexViewModel.DataGridHandleKeyDown(sender, e);
    }
}
