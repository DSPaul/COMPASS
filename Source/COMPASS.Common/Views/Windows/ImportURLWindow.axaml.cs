using Avalonia.Controls;
using COMPASS.Common.ViewModels.Import;

namespace COMPASS.Common.Views.Windows;

public partial class ImportURLWindow : Window
{
    public ImportURLWindow(ImportURLViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}