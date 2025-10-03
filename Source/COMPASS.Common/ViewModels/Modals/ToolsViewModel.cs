using System;
using System.Collections.ObjectModel;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.ViewModels.Tools;

namespace COMPASS.Common.ViewModels.Modals;

public class ToolsViewModel : ViewModelBase, IDisposable
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

    public void Dispose()
    {
        foreach (IToolViewModel tool in Tools)
        {
            if (tool is IDisposable disposableTool)
            {
                disposableTool.Dispose();
            }
        }
    }
}