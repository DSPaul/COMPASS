using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Operations;

namespace COMPASS.Common.ViewModels.Tools;

public class BrokenFileRefsToolViewModel : ViewModelBase, IToolViewModel
{
    private readonly ICodexCollectionStorageService _collectionStorageService;

    public BrokenFileRefsToolViewModel()
    {
        _collectionStorageService = ServiceResolver.Resolve<ICodexCollectionStorageService>();
    }

    #region IToolViewModel

    public string Name => "Fix broken references to local files";

    #endregion

    public IEnumerable<Codex> BrokenCodices => MainViewModel.CollectionVM.CurrentCollection.AllCodices
        .Where(codex => codex.Sources.HasOfflineSource()) //do this check so message doesn't count codices that never had a path to begin with
        .Where(codex => !Path.Exists(codex.Sources.Path));

    public int BrokenCodicesAmount => BrokenCodices.Count();
    public string BrokenCodicesMessage => $"Broken references detected: {BrokenCodicesAmount}.";

    private void BrokenCodicesChanged()
    {
        OnPropertyChanged(nameof(BrokenCodices));
        OnPropertyChanged(nameof(BrokenCodicesAmount));
        OnPropertyChanged(nameof(BrokenCodicesMessage));
    }

    private RelayCommand? _showBrokenCodicesCommand;
    public RelayCommand ShowBrokenCodicesCommand => _showBrokenCodicesCommand ??= new(ShowBrokenCodices);
    private void ShowBrokenCodices() => MainViewModel.CollectionVM.FilterVM.AddFilter(new HasBrokenPathFilter());

    //Rename the refs
    private int _amountRenamed = 0;

    public int AmountRenamed
    {
        get => _amountRenamed;
        set
        {
            SetProperty(ref _amountRenamed, value);
            OnPropertyChanged(nameof(RenameCompleteMessage));
            BrokenCodicesChanged();
        }
    }

    public string RenameCompleteMessage => $"Renamed path to fix reference on {AmountRenamed} items.";

    private RelayCommand<IList<object>>? _renameFolderRefCommand;
    public RelayCommand<IList<object>> RenameFolderRefCommand => _renameFolderRefCommand ??= new(RenameFolderReferences);

    private void RenameFolderReferences(IList<object>? args)
    {
        if (args is null || args.Count != 2)
        {
            return;
        }

        RenameFolderReferences(args[0] as string, args[1] as string);
    }

    private void RenameFolderReferences(string? oldPath, string? newPath)
    {
        if (string.IsNullOrWhiteSpace(oldPath) || newPath is null) return;

        AmountRenamed = 0;
        foreach (Codex codex in MainViewModel.CollectionVM.CurrentCollection.AllCodices)
        {
            if (!codex.Sources.HasOfflineSource() || //If no file referenced
                File.Exists(codex.Sources.Path) || //or reference file exists
                !codex.Sources.Path.Contains(oldPath)) //or path does not contain the substring that is being replaced
            {
                continue;
            }


            string updatedPath = codex.Sources.Path.Replace(oldPath, newPath);
            if (!File.Exists(updatedPath)) continue;
            codex.Sources.Path = updatedPath;
            AmountRenamed++;
        }

        _collectionStorageService.SaveCodices(MainViewModel.CollectionVM.CurrentCollection);
    }

    //remove refs from codices
    private RelayCommand? _removeBrokenRefsCommand;
    public RelayCommand RemoveBrokenRefsCommand => _removeBrokenRefsCommand ??= new(RemoveBrokenReferences);
    private void RemoveBrokenReferences()
    {
        foreach (Codex codex in BrokenCodices)
        {
            codex.Sources.Path = "";
        }

        BrokenCodicesChanged();
        _collectionStorageService.SaveCodices(MainViewModel.CollectionVM.CurrentCollection);
    }

    //Remove Codices with broken refs
    private AsyncRelayCommand? _removeCodicesWithBrokenRefsCommand;
    public AsyncRelayCommand RemoveCodicesWithBrokenRefsCommand => _removeCodicesWithBrokenRefsCommand ??= new(RemoveCodicesWithBrokenRefs);
    private async Task RemoveCodicesWithBrokenRefs()
    {
        await CodexOperations.DeleteCodices(BrokenCodices.ToList(), true);
        BrokenCodicesChanged();
    }
}