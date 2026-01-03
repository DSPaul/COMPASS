using System.Collections.ObjectModel;
using System.Collections.Specialized;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Models;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Main;

public class CodexCollectionVM : ModelViewModelBase<CodexCollection>
{
    public CodexCollectionVM(string identifier, CodexCollection collection, ICodexCollectionStorageService storageService) : base(collection)
    {
        _identifier = identifier;
            
        collection.AllCodices.CollectionChanged += OnAllCodicesCollectionChanged;
        
        _storageService = storageService;
        _notificationService = ServiceResolver.Resolve<INotificationService>();
    }

    private void OnAllCodicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var oldCodices = e.OldItems?.Cast<Codex>() ?? [];
        var oldCodexVms = AllCodexVms.Where(vm => oldCodices.Contains(vm.GetModel())).ToList();
        foreach (var codexVm in oldCodexVms)
        {
            AllCodexVms.Remove(codexVm);
            codexVm.Dispose();
        }
                
        var newCodices = e.NewItems?.Cast<Codex>() ?? [];
        AllCodexVms.AddRange(newCodices.Select(codex => new CodexViewModel(codex, this)));
    }

    private readonly ICodexCollectionStorageService _storageService;
    private readonly INotificationService _notificationService;

    public CodexCollection Collection => _model;
    public RangeObservableCollection<CodexViewModel> AllCodexVms { get; } = [];
    private Dictionary<Tag,TagViewModel> AllTagVms { get; } = [];
        
    /// <summary>
    /// A string that identifies this collection, such as its path
    /// </summary>
    private string _identifier;
    public string Identifier 
    { 
        get =>  _identifier; 
        private set => SetProperty(ref _identifier, value); 
    }

    /// <summary>
    /// The owners of the collection 
    /// </summary>
    public IList<CollectionHandle> Owners { get; } = [];
        
    public bool IsLoaded => Owners.Any();

    #region Load-Save Methods
    
    public CollectionHandle? Load()
    {
        var handle = new CollectionHandle(this);
        
        if (IsLoaded)
        {
            //Already loaded by an existing owner, no need to load it again
            Owners.Add(handle);
            return handle;
        }
            
        int loadResult = _storageService.Load(Collection);
        if (loadResult == 0) //0 means success
        {
            Owners.Add(handle);
            
            //Create vms
            AllCodexVms.AddRange(Collection.AllCodices.Select(x => new CodexViewModel(x, this)));
            return handle;
        }
        else if (loadResult < 0)
        {
            string msg = loadResult switch
            {
                -1 => "The save file for the Tags seems to be corrupted and could not be read.",
                -2 => "The save file with all items seems to be corrupted and could not be read.",
                -3 => "Both the save files with tags and items seem to be corrupted and could not be read.",
                _ => ""
            };
            Notification error = new("Failed to Load Collection", $"Could not load {Collection.Name}. \n" + msg, Severity.Error);
            _notificationService.ShowDialog(error);
        }
        
        return null;
    }

    public void Unload(CollectionHandle handle)
    {
        Owners.Remove(handle);

        //if no more owners, collection can be unloaded
        if (!Owners.Any())
        {
            _storageService.Unload(Collection);
            foreach (CodexViewModel codexVm in AllCodexVms)
            {
                codexVm.Dispose();
            }
            AllCodexVms.Clear();

            foreach (TagViewModel tagVm in AllTagVms.Values)
            {
                tagVm.Dispose();
            }
            AllTagVms.Clear();
        }
    }

    public void Save(CollectionHandle handle)
    {
        if (Owners.Contains(handle))
        {
            _storageService.Save(Collection);
        }
        else
        {
            Logger.Warn($"Someone tried to save with expired handle");
        }
    }
    
    public void SaveCodices(CollectionHandle handle)
    {
        if (Owners.Contains(handle))
        {
            _storageService.SaveCodices(Collection);
        }
        else
        {
            Logger.Warn($"Someone tried to save codices with expired handle");
        }
    }
    #endregion

    public TagViewModel GetTagVm(Tag tag)
    {
        if (!AllTagVms.TryGetValue(tag, out TagViewModel? tagVm))
        {
            tagVm = new(tag, this);
            AllTagVms.Add(tag, tagVm);
        }
        
        return tagVm;
    }
    
    public async Task AutoImport()
    {
        //Start Auto Imports
        using ImportFilesViewModel folderImportVM = new(autoImport: true);
        folderImportVM.NonRecursiveDirectories = Collection.Info.AutoImportFolders.Flatten().Select(f => f.FullPath).ToList() ?? [];
        await Task.Delay(TimeSpan.FromSeconds(2));
        await folderImportVM.Import();
    }
    
    public void RenameCollection(string newCollectionName)
    {
        string oldName = Collection.Name;
        Collection.Name = newCollectionName;
        Identifier = Collection.Name;
        
        //TODO, check if CollectionManager should be notified of name changes for AllCollectionNames list
        
        _storageService.OnCollectionRenamed(oldName, newCollectionName);
        ServiceResolver.Resolve<IThumbnailStorageService>().OnCollectionRenamed(Collection);

        Logger.Info($"Renamed {oldName} to {newCollectionName}");
    }

    private bool CanDeleteCollection()
    {
        if (Owners.Count > 0)
        {
            Logger.Warn("The collection is still in use");
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Delete all data related to the collection
    /// </summary>
    /// <returns></returns>
    public bool DeleteCollection()
    {
        //TODO cancel any background process that is using the collection like auto import
        
        if (!CanDeleteCollection()) return false;
        
        CollectionManager.RemoveCollection(this);
        _storageService.DeleteCollection(Identifier);
        return true;
    }

    public async Task ExportTags()
    {
        await _storageService.ExportTags(Collection);
    }
}
