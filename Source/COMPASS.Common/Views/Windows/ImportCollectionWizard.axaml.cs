using Avalonia.Controls;
using COMPASS.Common.ViewModels.Import;

namespace COMPASS.Common.Views.Windows;

public partial class ImportCollectionWizard : Window
{
    public ImportCollectionWizard(ImportCollectionViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}