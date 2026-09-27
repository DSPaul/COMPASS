using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.ViewModels.Tools;
using COMPASS.Infra.DependencyInjection;
using System.Collections.ObjectModel;

namespace COMPASS.Common.ViewModels.Modals;

public class ToolsViewModel : ViewModelBase, IDisposable
{
    public ToolsViewModel(BackupToolViewModelFactory backupToolViewModelFactory, BrokenFileRefsToolViewModelFactory brokenFileRefsToolViewModelFactory)
    {
        Tools =
        [
            backupToolViewModelFactory.Create(),
            brokenFileRefsToolViewModelFactory.Create()
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

[Factory]
public class ToolsViewModelFactory(
    BackupToolViewModelFactory backupToolViewModelFactory,
    BrokenFileRefsToolViewModelFactory brokenFileRefsToolViewModelFactory)
{
    public ToolsViewModel Create()
        => new(backupToolViewModelFactory, brokenFileRefsToolViewModelFactory);
}