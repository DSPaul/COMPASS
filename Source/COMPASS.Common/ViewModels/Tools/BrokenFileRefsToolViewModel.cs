using System;
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
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Common.ViewModels.Tools;

public class BrokenFileRefsToolViewModel : ViewModelBase, IToolViewModel, IDisposable
{
    public BrokenFileRefsToolViewModel()
    {
        SelectedCollectionVm = CollectionManager.CollectionVms.SingleOrDefault(vm => vm.Identifier == ActiveCollection.Name);
    }

    #region IToolViewModel

    public string Name => "Fix broken references to local files";

    #endregion

    private CollectionHandle? _selectedCollectionHandle;

    //TODO show a dropdown in the UI somewhere
    private CodexCollectionVM? _selectedCollectionVm;
    public CodexCollectionVM? SelectedCollectionVm
    {
        get => _selectedCollectionVm;
        set
        {
            _selectedCollectionHandle?.Dispose();
            SetProperty(ref _selectedCollectionVm, value);
            _selectedCollectionHandle = _selectedCollectionVm?.Load();
        }
    }
    
    public IEnumerable<Codex> BrokenCodices => SelectedCollectionVm?.Collection.AllCodices
        .Where(codex => codex.Sources.HasOfflineSource()) //do this check so the message doesn't count codices that never had a path to begin with
        .Where(codex => !Path.Exists(codex.Sources.Path)) ?? [];

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
    private void ShowBrokenCodices()
    {
        if (SelectedCollectionVm is null || _selectedCollectionHandle is null) return;
        
        //TODO: if the collection is already open in a tab with no filters, switch to it and apply
        //if not, open a new tab to apply the filter
        //Always open new tab for now
        var tabsVm = TabsViewModel.GetInstance();
        tabsVm.CreateTab(SelectedCollectionVm);
        tabsVm.ActiveTab!.FilterVM.AddFilter(new HasBrokenPathFilter());
    }

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
        if (SelectedCollectionVm is null || _selectedCollectionHandle is null) return;
        
        if (string.IsNullOrWhiteSpace(oldPath) || newPath is null) return;

        AmountRenamed = 0;
        foreach (Codex codex in SelectedCollectionVm.Collection.AllCodices)
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

        _selectedCollectionHandle.SaveCodices();
    }

    //remove refs from codices
    private RelayCommand? _removeBrokenRefsCommand;
    public RelayCommand RemoveBrokenRefsCommand => _removeBrokenRefsCommand ??= new(RemoveBrokenReferences);
    private void RemoveBrokenReferences()
    {
        if (SelectedCollectionVm is null || _selectedCollectionHandle is null) return;
        
        foreach (Codex codex in BrokenCodices)
        {
            codex.Sources.Path = "";
        }

        BrokenCodicesChanged();
        _selectedCollectionHandle.SaveCodices();
    }

    //Remove Codices with broken refs
    private AsyncRelayCommand? _removeCodicesWithBrokenRefsCommand;
    public AsyncRelayCommand RemoveCodicesWithBrokenRefsCommand => _removeCodicesWithBrokenRefsCommand ??= new(RemoveCodicesWithBrokenRefs);
    private async Task RemoveCodicesWithBrokenRefs()
    {
        if (SelectedCollectionVm is null || _selectedCollectionHandle is null) return;
        
        await new CodexOperations(_selectedCollectionHandle).DeleteCodices(BrokenCodices.ToList(), true);
        BrokenCodicesChanged();
    }

    public void Dispose()
    {
        _selectedCollectionHandle?.Dispose();
    }
}