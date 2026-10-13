using COMPASS.Infra.DependencyInjection;
using System.Collections.ObjectModel;
using COMPASS.Infra.Avalonia.Validation;

namespace COMPASS.Common.Features.Tools;

public class ToolsViewModel : ValidatableViewModelBase, IDisposable
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