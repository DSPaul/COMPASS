using Avalonia.Controls;
using COMPASS.Common.ViewModels.Import;

namespace COMPASS.Common;

public partial class ImportFolderWizard : Window
{
    public ImportFolderWizard(ImportFolderViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}