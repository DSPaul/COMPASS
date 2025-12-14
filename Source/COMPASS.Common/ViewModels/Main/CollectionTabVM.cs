using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Layouts;
using COMPASS.Common.ViewModels.SidePanels;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Main;

public class CollectionTabVM : ViewModelBase, IDisposable
{ 
    public CollectionTabVM() : 
        this(CollectionManager.GetOrCreateInitialCollectionVM().Result)
    { }

    public CollectionTabVM(CodexCollectionVM collectionVm, FiltersState? filtersState = null, CodexLayout? layout = null) : 
        this(collectionVm.Load() ?? CollectionManager.GetOrCreateInitialCollectionVM().Result, filtersState, layout)
    { }
    
    public CollectionTabVM(CollectionHandle collectionHandle, FiltersState? filtersState = null, CodexLayout? layout = null)
    {
        _collectionHandle = collectionHandle;
        
        _filtersVM = new(_collectionHandle.CollectionVM.AllCodexVms, filtersState);
        _tagsVM = new(_collectionHandle.CollectionVM, _filtersVM);
        _currentLayout = LayoutViewModel.GetLayout(layout);
        CodexCommands = new();
    }

    #region events

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
    
    //show edit Collection Stuff
    private bool _createCollectionVisibility = false;
    public bool CreateCollectionVisibility
    {
        get => _createCollectionVisibility;
        set => SetProperty(ref _createCollectionVisibility, value);
    }

    //show edit Collection Stuff
    private bool _editCollectionVisibility = false;
    public bool EditCollectionVisibility
    {
        get => _editCollectionVisibility;
        set => SetProperty(ref _editCollectionVisibility, value);
    }

    #endregion
    
    #region Commands and their methods

    public async Task Refresh()
    {
        await ChangeToCollection(_collectionHandle.CollectionVM);
    }

    private RelayCommand? _toggleCreateCollectionCommand;
    public RelayCommand ToggleCreateCollectionCommand => _toggleCreateCollectionCommand ??= new(ToggleCreateCollection);
    private void ToggleCreateCollection() => CreateCollectionVisibility = !CreateCollectionVisibility;

    private RelayCommand? _toggleEditCollectionCommand;
    public RelayCommand ToggleEditCollectionCommand => _toggleEditCollectionCommand ??= new(ToggleEditCollection);
    private void ToggleEditCollection() => EditCollectionVisibility = !EditCollectionVisibility;

    // Create CodexCollection
    private AsyncRelayCommand<string>? _createCollectionCommand;
    public AsyncRelayCommand<string> CreateCollectionCommand => _createCollectionCommand ??= new(
        CreateCollection, 
        name => CollectionManager.IsLegalCollectionName(name));
    private async Task CreateCollection(string? name)
    {
        CollectionHandle? newCollectionHandle = await CollectionManager.CreateAndLoadCollection(name);
        if (newCollectionHandle != null)
        {
            CreateCollectionVisibility = false;
            await ChangeToCollection(newCollectionHandle);
        }
    }

    // Rename Collection
    private RelayCommand<string>? _editCollectionNameCommand;
    public RelayCommand<string> EditCollectionNameCommand => _editCollectionNameCommand ??= new(
        EditCollectionName,
        name => CollectionManager.IsLegalCollectionName(name));
    private void EditCollectionName(string? newName)
    {
        if (!CollectionManager.IsLegalCollectionName(newName)) return;

        CollectionVM.RenameCollection(newName!);
        EditCollectionVisibility = false;
    }

    // Delete Collection
    private AsyncRelayCommand? _deleteCollectionCommand;
    public AsyncRelayCommand DeleteCollectionCommand => _deleteCollectionCommand ??= new(RaiseDeleteCollectionWarning);
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

            var windowedNotificationService = ServiceResolver.Resolve<INotificationService>();
            await windowedNotificationService.ShowDialog(areYouSure);

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
            if (PreferencesService.GetInstance().Preferences.UIState.StartupCollection == collectionToDelete.Identifier)
            {
                PreferencesService.GetInstance().Preferences.UIState.StartupCollection =
                    CollectionManager.CollectionVms
                        .Select(vm => vm.Identifier)
                        .FirstOrDefault(vm => vm != collectionToDelete.Identifier) ?? "Default Collection";
            }

            //Switch to another collection
            var collectionHandle = await CollectionManager.GetOrCreateInitialCollectionVM();
            await ChangeToCollection(collectionHandle, saveBeforeSwitch: false);
        }
        else
        {
            Logger.Warn($"Failed to delete {collectionToDelete.Identifier}");
        }
    }

    //Export Collection
    private RelayCommand? _exportCommand;
    public RelayCommand ExportCommand => _exportCommand ??= new(Export);
    private void Export()
    {
        //open wizard
        ExportCollectionViewModel exportCollectionVM = new(CollectionVM.Collection);
        ExportCollectionWizard wizard = new(exportCollectionVM);
        wizard.Show(WindowManager.ActiveWindow);
    }

    private AsyncRelayCommand? _exportTagsCommand;
    public AsyncRelayCommand ExportTagsCommand => _exportTagsCommand ??= new(CollectionVM.ExportTags);

    //Import Collection
    private AsyncRelayCommand? _importCommand;
    public AsyncRelayCommand ImportCommand => _importCommand ??= new(ImportSatchelAsync);

    private async Task ImportSatchelAsync() => await ImportSatchelAsync(null);
    public async Task ImportSatchelAsync(string? path)
    {
        //satchels contain data in xml format
        var storageService = ServiceResolver.ResolveKeyed<ICodexCollectionStorageService>(StorageStrategy.Xml);
        var extractedCollectionName = await storageService.OpenSatchel(path);

        if (extractedCollectionName == null)
        {
            Logger.Warn("Failed to read file");
            return;
        }
        
        //open wizard, which will handle the rest of the import process
        CodexCollection toImport = new(extractedCollectionName);
        CodexCollectionVM toImportVm = new(extractedCollectionName, toImport, storageService);
        ImportCollectionViewModel importCollectionVM = new(toImportVm);
        ModalWindow wizard = new(importCollectionVM);
        wizard.Show(WindowManager.ActiveWindow);
    }

    //Merge Collection into another
    private AsyncRelayCommand<string>? _mergeCollectionIntoCommand;
    public AsyncRelayCommand<string> MergeCollectionIntoCommand => _mergeCollectionIntoCommand ??= new(MergeIntoCollection);
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
        await ServiceResolver.Resolve<INotificationService>().ShowDialog(areYouSure);
        if (areYouSure.Result != NotificationAction.Confirm) return;

        //load target, merge, and unload
        using (var targetCollectionHandle = CollectionManager.LoadCollection(collectionToMergeInto))
        {
            if (targetCollectionHandle == null)
            {
                Logger.Warn($"Failed to load merge {CollectionVM.Identifier} into {collectionToMergeInto} " +
                            $"because the target collection could not be loaded.");
                return;
            }
            
            targetCollectionHandle.CollectionVM.Collection.MergeWith(CollectionVM.Collection);
            targetCollectionHandle.Save();
        }

        Notification doneNotification = new("Merge Success", $"Successfully merged '{CollectionVM.Identifier}' into '{collectionToMergeInto}'");

        //TODO toast notifications
        //await ServiceResolver.Resolve<INotificationService>().ShowToast(doneNotification);
    }
    
    //Change Layout
    private RelayCommand<CodexLayout>? _changeLayoutCommand;
    public RelayCommand<CodexLayout> ChangeLayoutCommand => _changeLayoutCommand ??= new(ChangeLayout);
    private void ChangeLayout(CodexLayout layout) => CurrentLayout = LayoutViewModel.GetLayout(layout);
    
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
        PreferencesService.GetInstance().Preferences.UIState.StartupCollection = _collectionHandle.CollectionVM.Identifier;
        
        //TODO: check if this is still needed
        //CurrentLayout?.UpdateDoVirtualization();
        
        FiltersVM = new(newHandle.CollectionVM.AllCodexVms);
        TagsVM = new(newHandle.CollectionVM, FiltersVM);
        CodexCommands = new();
        
        FiltersVM.ReFilter(true);

        OnPropertyChanged(nameof(CollectionVM));
        CollectionChanged?.Invoke(this, EventArgs.Empty);

        await newHandle.CollectionVM.AutoImport();
    }
    
    
    private void OnCollectionsChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(AllCodexCollections));
    }
    #endregion

    #region IDisposable
    
    public void Dispose()
    {
        _collectionHandle.Dispose();
    }

    #endregion
}