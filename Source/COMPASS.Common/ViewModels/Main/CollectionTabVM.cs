using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Operations;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Layouts;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Common.ViewModels.Modals.Edit;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Common.ViewModels.SidePanels;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Common.Models.Preferences;

namespace COMPASS.Common.ViewModels.Main;

public class CollectionTabVM : ViewModelBase, IDisposable
{
    private readonly ILogger _logger;
    private readonly INotificationService _notificationService;
    private readonly IImportExportService _importExportService;
    private readonly CodexCollectionOperations _codexCollectionOperations;
    private readonly CodexOperations _codexOperations;
    private readonly CodexCollectionVMFactory _codexCollectionVMFactory;
    private readonly FiltersViewModelFactory _filtersViewModelFactory;
    private readonly TagsPanelVMFactory _tagsPanelVMFactory;
    private readonly LayoutViewModelFactory _layoutViewModelFactory;
    private readonly ImportCollectionViewModelFactory _importCollectionViewModelFactory;
    private readonly ExportCollectionViewModelFactory _exportCollectionViewModelFactory;
    private readonly UIState _uiState;

    public CollectionTabVM(
        ILogger logger,
        INotificationService notificationService,
        IPreferencesService preferencesService,
        IImportExportService importExportService,
        CodexCollectionOperations codexCollectionOperations,
        CodexOperations codexOperations,
        CodexCollectionVMFactory codexCollectionVMFactory,
        FiltersViewModelFactory filtersViewModelFactory,
        TagsPanelVMFactory tagsPanelVMFactory,
        LayoutViewModelFactory layoutViewModelFactory,
        ImportCollectionViewModelFactory importCollectionViewModelFactory,
        ExportCollectionViewModelFactory exportCollectionViewModelFactory,
        CollectionHandle collectionHandle, FiltersState? filtersState = null, CodexLayout? layout = null)
    {
        _logger = logger;
        _notificationService = notificationService;
        _importExportService = importExportService;
        _codexCollectionOperations = codexCollectionOperations;
        _codexOperations = codexOperations;
        CodexCommands = codexOperations;
        _codexCollectionVMFactory = codexCollectionVMFactory;
        _filtersViewModelFactory = filtersViewModelFactory;
        _tagsPanelVMFactory = tagsPanelVMFactory;
        _layoutViewModelFactory = layoutViewModelFactory;
        _importCollectionViewModelFactory = importCollectionViewModelFactory;
        _exportCollectionViewModelFactory = exportCollectionViewModelFactory;
        _uiState = preferencesService.Preferences.UIState;
        _collectionHandle = collectionHandle;

        _filtersVM = filtersViewModelFactory.Create(_collectionHandle.CollectionVM, filtersState);
        _tagsVM = tagsPanelVMFactory.Create(_collectionHandle.CollectionVM, _filtersVM);
        _currentLayout = layoutViewModelFactory.Create(this, layout);
        CodexCommands = _codexOperations;
    }

    #region events

    public event EventHandler? CollectionChanging;
    public event EventHandler? CollectionChanged;
    
    #endregion
    
    #region Properties
    
    private CollectionHandle _collectionHandle;
    
    public CodexCollectionVM CollectionVM => _collectionHandle.CollectionVM;
    
    public IReadOnlyCollection<CodexCollectionVM> AllCodexCollections => CollectionManager.CollectionVms;
    
    private FiltersViewModel _filtersVM;
    public FiltersViewModel FiltersVM
    {
        get => _filtersVM;
        private set => SetProperty(ref _filtersVM, value);
    }

    private TagsPanelVM _tagsVM;
    public TagsPanelVM TagsVM
    {
        get => _tagsVM;
        private set => SetProperty(ref _tagsVM, value);
    }
    
    private LayoutViewModel _currentLayout;
    public LayoutViewModel CurrentLayout
    {
        get => _currentLayout;
        private set => SetProperty(ref _currentLayout, value);
    }
    
    //TODO: commands should be in a viewmodel rather than operations
    public CodexOperations CodexCommands { get; private set; }

    #endregion

    #region Commands and their methods

    public async Task Refresh() => await ChangeToCollection(_collectionHandle.CollectionVM);

    // Create CodexCollection
    public AsyncRelayCommand CreateCollectionCommand => field ??= new(CreateCollection);
    private async Task CreateCollection() => await WindowManager.OpenModal(new CollectionEditViewModel(CollectionVM, createNew: true));

    // Rename Collection
    public AsyncRelayCommand RenameCollectionCommand => field ??= new(RenameCollection);
    private async Task RenameCollection() => await WindowManager.OpenModal(new CollectionEditViewModel(CollectionVM, createNew: false));

    // Delete Collection
    public AsyncRelayCommand DeleteCollectionCommand => field ??= new(RaiseDeleteCollectionWarning);
    private async Task RaiseDeleteCollectionWarning()
    {
        int codexCount = CollectionVM.Collection.AllCodices.Count;
        
        if (codexCount > 0)
        {
            //"Are you Sure?"

            const string messageSingle = "There is still one item in this collection, if you don't want to remove it from COMPASS, move it to another collection first. Are you sure you want to continue?";
            string messageMultiple = $"There are still {codexCount} items in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";

            Notification areYouSure = Notification.AreYouSureNotification;
            areYouSure.Body = codexCount == 1 ? messageSingle : messageMultiple;

            await _notificationService.ShowDialog(areYouSure);

            if (areYouSure.Result == NotificationAction.Confirm)
            {
               await OnConfirmedDelete();
            }
        }
        else
        {
            await OnConfirmedDelete();
        }
    }

    private async Task OnConfirmedDelete()
    {
        var collectionToDelete = CollectionVM;
        
        _collectionHandle.Dispose();
        bool deleted = collectionToDelete.DeleteCollection();

        if (deleted)
        {
            //If it was the default, change the default
            if (_uiState.StartupCollection == collectionToDelete.Identifier)
            {
                _uiState.StartupCollection =
                    CollectionManager.CollectionVms
                        .Select(vm => vm.Identifier)
                        .FirstOrDefault(vm => vm != collectionToDelete.Identifier) ?? Constants.DEFAULT_COLLECTION_NAME;
            }

            //Switch to another collection
            var collectionHandle = CollectionManager.GetOrCreateInitialCollectionVM();
            await ChangeToCollection(collectionHandle, saveBeforeSwitch: false);
        }
        else
        {
            _logger.Warn($"Failed to delete {collectionToDelete.Identifier}");
        }
    }

    //Export Collection
    public AsyncRelayCommand ExportCommand => field ??= new(Export);
    private async Task Export()
    {
        ExportCollectionViewModel exportCollectionVM = _exportCollectionViewModelFactory.Create(CollectionVM.Collection);
        ModalWindow wizard = new(exportCollectionVM);
        await wizard.ShowDialog(WindowManager.ActiveWindow);
    }

    public AsyncRelayCommand ExportTagsCommand => field ??= new(CollectionVM.ExportTags);

    //Import Collection
    public AsyncRelayCommand ImportCommand => field ??= new(ImportSatchelAsync);

    private async Task ImportSatchelAsync() => await ImportSatchelAsync(null);
    public async Task ImportSatchelAsync(string? path)
    {
        var extractedCollection = await _importExportService.OpenSatchel(path);

        if (extractedCollection == null)
        {
            _logger.Warn("Failed to read file");
            return;
        }
        
        //open wizard, which will handle the rest of the import process
        CodexCollectionVM toImportVm = _codexCollectionVMFactory.Create(extractedCollection, StorageStrategy.Xml);
        ImportCollectionViewModel importCollectionVM = _importCollectionViewModelFactory.Create(toImportVm);
        ModalWindow wizard = new(importCollectionVM);
        wizard.Show(WindowManager.ActiveWindow);
    }

    //Merge Collection into another
    public AsyncRelayCommand<string> MergeCollectionIntoCommand => field ??= new(MergeIntoCollection);
    private async Task MergeIntoCollection(string? collectionToMergeInto)
    {
        if (string.IsNullOrEmpty(collectionToMergeInto) ||
            !CollectionManager.CollectionExists(collectionToMergeInto))
        {
            return;
        }

        //Are you sure?
        Notification areYouSure = Notification.AreYouSureNotification;
        areYouSure.Title = "Confirm merge";
        areYouSure.Body = $"You are about to merge '{CollectionVM.Identifier}' into '{collectionToMergeInto}'. \n" +
                       $"This will copy all items, tags and preferences to the chosen collection. \n" +
                       $"Are you sure you want to continue?";
        await _notificationService.ShowDialog(areYouSure);
        if (areYouSure.Result != NotificationAction.Confirm) return;

        //load target, merge, and unload
        using (var targetCollectionHandle = CollectionManager.LoadCollection(collectionToMergeInto))
        {
            if (targetCollectionHandle == null)
            {
                _logger.Warn($"Failed to load merge {CollectionVM.Identifier} into {collectionToMergeInto} " +
                            $"because the target collection could not be loaded.");
                return;
            }
            
            _codexCollectionOperations.Merge(CollectionVM.Collection, targetCollectionHandle.CollectionVM.Collection);
            targetCollectionHandle.Save();
        }

        Notification doneNotification = new("Merge Success", $"Successfully merged '{CollectionVM.Identifier}' into '{collectionToMergeInto}'");

        //TODO toast notifications
        //await _notificationService.ShowToast(doneNotification);
    }
    
    //Change Layout
    public RelayCommand<CodexLayout> ChangeLayoutCommand => field ??= new(ChangeLayout);
    private void ChangeLayout(CodexLayout layout)
    {
        _currentLayout.Dispose();
        CurrentLayout = _layoutViewModelFactory.Create(this, layout);
    }

    #endregion
    
    #region Methods
    
    /// <summary>
    /// Called by selection changed event on collection dropdown
    /// </summary>
    public async Task ChangeToCollection(CodexCollectionVM collectionToChangeTo)
    {
        var newHandle = collectionToChangeTo.Load();

        if (newHandle == null)
        {
            //load failed TODO
            return;
        }

        await ChangeToCollection(newHandle);
    }

    public async Task ChangeToCollection(CollectionHandle newHandle, bool saveBeforeSwitch = true)
    {
        if (saveBeforeSwitch)
        {
            //save prev collection before switching
            _collectionHandle.Save();
        }
        
        _collectionHandle.Dispose();
        _collectionHandle = newHandle;
        
        //update Startup collection, TODO make this a setting, choose between a set collection or last used (current behaviour)
        _uiState.StartupCollection = _collectionHandle.CollectionVM.Identifier;
        
        
        CollectionChanging?.Invoke(this, EventArgs.Empty);
        FiltersVM = _filtersViewModelFactory.Create(newHandle.CollectionVM);
        TagsVM = _tagsPanelVMFactory.Create(newHandle.CollectionVM, FiltersVM);

        OnPropertyChanged(nameof(CollectionVM));
        CollectionChanged?.Invoke(this, EventArgs.Empty);

        await newHandle.CollectionVM.AutoImport();
    }
    
    #endregion

    #region IDisposable
    
    public void Dispose()
    {
        _collectionHandle.Dispose();
        _currentLayout.Dispose();
    }

    #endregion
}

[Factory]
public class CollectionTabVMFactory(
    ILogger logger,
    INotificationService notificationService,
    IPreferencesService preferencesService,
    IImportExportService importExportService,
    CodexCollectionOperations codexCollectionOperations,
    CodexOperations codexOperations,
    CodexCollectionVMFactory codexCollectionVMFactory,
    FiltersViewModelFactory filtersViewModelFactory,
    TagsPanelVMFactory tagsPanelVMFactory,
    LayoutViewModelFactory layoutViewModelFactory,
    ImportCollectionViewModelFactory importCollectionViewModelFactory,
    ExportCollectionViewModelFactory exportCollectionViewModelFactory)
{
    public CollectionTabVM Create(CollectionHandle collectionHandle, FiltersState? filtersState = null, CodexLayout? layout = null)
        => new(logger, notificationService, preferencesService, importExportService, codexCollectionOperations, codexOperations,
               codexCollectionVMFactory, filtersViewModelFactory, tagsPanelVMFactory,
               layoutViewModelFactory, importCollectionViewModelFactory, exportCollectionViewModelFactory,
               collectionHandle, filtersState, layout);
}