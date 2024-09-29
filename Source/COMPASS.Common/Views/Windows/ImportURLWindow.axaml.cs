using Avalonia.Controls;
using COMPASS.Common.ViewModels.Import;

namespace COMPASS.Common;

public partial class ImportURLWindow : Window
{
    public ImportURLWindow(ImportURLViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}