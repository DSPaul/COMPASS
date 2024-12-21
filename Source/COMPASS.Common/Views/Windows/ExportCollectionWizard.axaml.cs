using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class ExportCollectionWizard : Window
{
    public ExportCollectionWizard(ExportCollectionViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}