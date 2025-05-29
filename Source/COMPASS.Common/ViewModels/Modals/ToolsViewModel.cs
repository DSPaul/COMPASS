using System.Collections.ObjectModel;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.ViewModels.Tools;

namespace COMPASS.Common.ViewModels.Modals;

public class ToolsViewModel : ViewModelBase
{
    public ToolsViewModel()
    {
        Tools =
        [
            new BackupToolViewModel(),
            new BrokenFileRefsToolViewModel()
        ];
    }
    
    public ObservableCollection<IToolViewModel> Tools { get; }
}