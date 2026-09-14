using System.Collections.Specialized;
using System.ComponentModel;
using COMPASS.Common.Interfaces.Repos;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools.Logging;
using Autofac.Features.Indexed;

namespace COMPASS.Common.ViewModels.Main;

public class CodexCollectionVM : ModelViewModelBase<CodexCollection>
{
    private readonly ILogger _logger;
    private readonly ICodexCollectionRepository _repo;
    private readonly INotificationService _notificationService;
    private readonly IImportExportService _importExportService;
    private readonly ICoverStorageService _coverStorageService;
    private readonly CodexViewModelFactory _codexViewModelFactory;
    private readonly TagViewModelFactory _tagViewModelFactory;
    private readonly ImportFilesViewModelFactory _importFilesViewModelFactory;
    private readonly CollectionManager _collectionManager;

    public CodexCollectionVM(string identifier, CodexCollection collection, 
        ICodexCollectionRepository repo, ILogger logger, INotificationService notificationService, IImportExportService importExportService, 
        ICoverStorageService coverStorageService, CodexViewModelFactory codexViewModelFactory, TagViewModelFactory tagViewModelFactory,
        ImportFilesViewModelFactory importFilesViewModelFactory, CollectionManager collectionManager)
        : base(collection)
    {
        _logger = logger;
        _notificationService = notificationService;
        _importExportService = importExportService;
        _coverStorageService = coverStorageService;
        _codexViewModelFactory = codexViewModelFactory;
        _tagViewModelFactory = tagViewModelFactory;
        _importFilesViewModelFactory = importFilesViewModelFactory;
        _collectionManager = collectionManager;

        _identifier = identifier;
        _repo = repo;

        //Create codex vms for existing codices in collection
        AllCodexVms.AddRange(collection.AllCodices.Select(codex => _codexViewModelFactory.Create(codex, this)));
        collection.AllCodices.CollectionChanged += OnAllCodicesCollectionChanged;
    }

    public EventHandler<PropertyChangedEventArgs>? CodexPropertyChanged;

    private void OnAllCodicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var oldCodices = e.OldItems?.Cast<Codex>() ?? [];
        var oldCodexVms = AllCodexVms.Where(vm => oldCodices.Contains(vm.GetModel())).ToList();
        foreach (var codexVm in oldCodexVms)
        {
            AllCodexVms.Remove(codexVm);
            codexVm.PropertyChanged -= OnCodexPropertyChanged;
            codexVm.Dispose();
        }

        var newCodices = (e.NewItems?.Cast<Codex>() ?? [])
            .Select(codex => _codexViewModelFactory.Create(codex, this))
            .ToList();
        foreach (var codexVm in newCodices)
        {
            codexVm.PropertyChanged += OnCodexPropertyChanged;
        }

        AllCodexVms.AddRange(newCodices);
    }

    // Route all codex property changed events through the collection vm
    // to make it easier to listen for changes on the codices from the outside
    private void OnCodexPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not CodexViewModel codexVm) return;
        CodexPropertyChanged?.Invoke(codexVm, e);
    }

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
            
        int loadResult = _repo.Load(Collection);
        if (loadResult == 0) //0 means success
        {
            Owners.Add(handle);
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
            _notificationService.Notify(error);
        }
        
        return null;
    }

    public void Unload(CollectionHandle handle)
    {
        Owners.Remove(handle);

        //if no more owners, collection can be unloaded
        if (!Owners.Any())
        {
            _repo.Unload(Collection);
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
            _repo.Save(Collection);
        }
        else
        {
            _logger.Warn($"Someone tried to save with expired handle");
        }
    }
    
    public void SaveCodices(CollectionHandle handle)
    {
        if (Owners.Contains(handle))
        {
            _repo.SaveCodices(Collection);
        }
        else
        {
            _logger.Warn($"Someone tried to save codices with expired handle");
        }
    }
    #endregion

    public TagViewModel GetTagVm(Tag tag)
    {
        if (!AllTagVms.TryGetValue(tag, out TagViewModel? tagVm))
        {
            tagVm = _tagViewModelFactory.Create(tag, this);
            AllTagVms.Add(tag, tagVm);
        }
        
        return tagVm;
    }
    
    public async Task AutoImport()
    {
        try
        {
            //Start Auto Imports
            using ImportFilesViewModel folderImportVM = _importFilesViewModelFactory.Create(autoImport: true);
            var autoImportFolders = Collection.Info.AutoImportFolders.Flatten();

            //Check for any new folders
            foreach (var folder in autoImportFolders)
            {
                folder.UpdateAllSubFolders();
            }

            folderImportVM.NonRecursiveDirectories = Collection.Info.AutoImportFolders.Flatten().Select(f => f.FullPath).ToList() ?? [];
            await Task.Delay(TimeSpan.FromSeconds(2));
            await folderImportVM.Import();
        }
        catch (Exception ex) 
        { 
            _logger.Error("Error during auto import", ex);
        }
    }
    
    public void RenameCollection(string newCollectionName)
    {
        string oldName = Collection.Name;
        Collection.Name = newCollectionName;
        Identifier = Collection.Name;
        
        _repo.OnCollectionRenamed(oldName, newCollectionName);
        _coverStorageService.OnCollectionRenamed(Collection);

        _logger.Info($"Renamed {oldName} to {newCollectionName}");
    }

    private bool CanDeleteCollection()
    {
        if (Owners.Count > 0)
        {
            _logger.Warn("The collection is still in use");
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
        
        _collectionManager.RemoveCollection(this);
        _repo.DeleteCollection(Identifier);
        return true;
    }

    public async Task ExportTags()
    {
        await _importExportService.ExportTags(Collection);
    }
}

[Factory]
public class CodexCollectionVMFactory(
    ILogger logger,
    INotificationService notificationService,
    IImportExportService importExportService,
    ICoverStorageService coverStorageService,
    IIndex<StorageStrategy, ICodexCollectionRepository> repoIndex,
    CodexViewModelFactory codexViewModelFactory,
    TagViewModelFactory tagViewModelFactory,
    ImportFilesViewModelFactory importFilesViewModelFactory,
    Lazy<CollectionManager> collectionManager)
{
    public CodexCollectionVM Create(CodexCollection collection, ICodexCollectionRepository repo)
        => new(collection.Name, collection, repo, logger, notificationService, importExportService, coverStorageService,
               codexViewModelFactory, tagViewModelFactory, importFilesViewModelFactory, collectionManager.Value);

    public CodexCollectionVM Create(CodexCollection collection, StorageStrategy storageStrategy)
        => Create(collection, repoIndex[storageStrategy]);
}
