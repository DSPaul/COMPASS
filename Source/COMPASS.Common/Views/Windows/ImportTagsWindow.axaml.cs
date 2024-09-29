using Avalonia.Controls;
using COMPASS.Common.ViewModels.Import;

namespace COMPASS.Common;

public partial class ImportTagsWindow : Window
{
    public ImportTagsWindow(ImportTagsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}