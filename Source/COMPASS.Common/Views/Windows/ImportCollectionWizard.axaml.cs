using Avalonia.Controls;
using COMPASS.Common.ViewModels.Import;

namespace COMPASS.Common;

public partial class ImportCollectionWizard : Window
{
    public ImportCollectionWizard(ImportCollectionViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}