using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common;

public partial class ExportCollectionWizard : Window
{
    public ExportCollectionWizard(ExportCollectionViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}