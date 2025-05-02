using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.SidePanels;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.ViewModels
{
    public class CollectionViewModel : ViewModelBase
    {
        public CollectionViewModel(MainViewModel? mainViewModel)
        {
            MainVM = mainViewModel;
            _windowedNotificationService = ServiceResolver.Resolve<INotificationService>();
            _collectionStorageService = ServiceResolver.Resolve<ICodexCollectionStorageService>();

            //only to avoid null references, should be overwritten as soon as the UI loads, which calls refresh
            _filterVM = new([]);
            _tagsVM = new(new("__tmp"), _filterVM);

            _collectionStorageService.EnsureDirectoryExists();
            AllCodexCollections = new(_collectionStorageService.GetAllCollections());
            LoadInitialCollection();

            Debug.Assert(_currentCollection is not null, "Current Collection should never be null after loading Initial Collection");
        }
        
        private readonly ICodexCollectionStorageService _collectionStorageService;
        private readonly INotificationService _windowedNotificationService;

        #region Properties
        public MainViewModel? MainVM { get; init; }

        private CodexCollection _currentCollection;
        public CodexCollection CurrentCollection
        {
            get => _currentCollection;
            set
            {
                if (value is null || value == _currentCollection)
                {
                    return;
                }
                //save prev collection before switching
                if (_currentCollection != null)
                {
                    _collectionStorageService.Save(_currentCollection);
                }

                if (SetProperty(ref _currentCollection, value))
                {
                    PreferencesService.GetInstance().Preferences.UIState.StartupCollection = _currentCollection.Name;
                }
            }
        }

        private ObservableCollection<CodexCollection> _allCodexCollections = [];
        public ObservableCollection<CodexCollection> AllCodexCollections
        {
            get => _allCodexCollections;
            init => SetProperty(ref _allCodexCollections, value);
        }

        //Needed for binding to context menu "Move to Collection"
        public ObservableCollection<string> CollectionDirectories => new(AllCodexCollections.Select(collection => collection.Name));

        private FilterViewModel _filterVM;
        public FilterViewModel FilterVM
        {
            get => _filterVM;
            private set => SetProperty(ref _filterVM, value);
        }

        private TagsPanelVM _tagsVM;
        public TagsPanelVM TagsVM
        {
            get => _tagsVM;
            set => SetProperty(ref _tagsVM, value);
        }

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

        #region Methods and Commands

        private void LoadInitialCollection()
        {
            var loadedCollection = CodexCollectionOperations.LoadInitialCollection(AllCodexCollections);
            
            if (loadedCollection == null)
            {
                Debug.Assert(AllCodexCollections.Count == 0, "Collection should only be null if all options have been tried and failed");
                string name = "Default Collection";
                var createdCollection = CreateAndLoadCollection(name);
                if (createdCollection == null)
                {
                    //If no collections are found and creation fails, we are stuck in an infinite loop which is bad so throw and crash
                    throw new IOException($"Could not create the default collection");
                }
            }
            else
            {
                CurrentCollection = loadedCollection;
            }
        }
        
        /// <summary>
        /// Tries to load a collection and will set <see cref="CurrentCollection"/> if succesfull
        /// </summary>
        /// <param name="collection"></param>
        /// <returns>A boolean indiciating whether or not the load was a succes </returns>
        private bool TryLoadCollection(CodexCollection collection)
        {
            int success = _collectionStorageService.Load(collection);
            if (success < 0)
            {
                string msg = success switch
                {
                    -1 => "The save file for the Tags seems to be corrupted and could not be read.",
                    -2 => "The save file with all items seems to be corrupted and could not be read.",
                    -3 => "Both the save files with tags and items seem to be corrupted and could not be read.",
                    _ => ""
                };
                Notification error = new("Failed to Load Collection", $"Could not load {collection.Name}. \n" + msg, Severity.Error);
                _windowedNotificationService.ShowDialog(error);
                return false;
            }

            CurrentCollection = collection;
            return true;
        }
        
        public async Task AutoImport()
        {
            //Start Auto Imports
            ImportFilesViewModel folderImportVM = new(autoImport: true)
            {
                NonRecursiveDirectories = CurrentCollection?.Info.AutoImportFolders.Flatten().Select(f => f.FullPath).ToList() ?? [],
            };
            await Task.Delay(TimeSpan.FromSeconds(2));
            await folderImportVM.Import();
        }

        public async Task Refresh()
        {
            _collectionStorageService.Save(CurrentCollection);

            await OnCollectionChanged();
        }

        public async Task OnCollectionChanged()
        {
            bool loaded = TryLoadCollection(CurrentCollection);

            if (!loaded)
            {
                //something must have gone wrong during save right before
                //highly unlikely, look at this later
                //TODO
                return;
            }
            //TODO: check if this is still needed
            //MainVM?.CurrentLayout?.UpdateDoVirtualization();
            
            FilterVM = new(CurrentCollection.AllCodices);
            TagsVM = new(CurrentCollection, FilterVM);

            FilterVM.ReFilter(true);

            await AutoImport();
        }

        private RelayCommand? _toggleCreateCollectionCommand;
        public RelayCommand ToggleCreateCollectionCommand => _toggleCreateCollectionCommand ??= new(ToggleCreateCollection);
        private void ToggleCreateCollection() => CreateCollectionVisibility = !CreateCollectionVisibility;

        private RelayCommand? _toggleEditCollectionCommand;
        public RelayCommand ToggleEditCollectionCommand => _toggleEditCollectionCommand ??= new(ToggleEditCollection);
        private void ToggleEditCollection() => EditCollectionVisibility = !EditCollectionVisibility;

        // Create CodexCollection
        private AsyncRelayCommand<string>? _createCollectionCommand;
        public AsyncRelayCommand<string> CreateCollectionCommand =>
            _createCollectionCommand ??= new(
                name => CreateAndLoadCollection(name), 
                name => CodexCollectionOperations.IsLegalCollectionName(name, AllCodexCollections));
        public async Task<CodexCollection?> CreateAndLoadCollection(string? dirName)
        {
            if (dirName == null)
            {
                return null;
            }

            CodexCollection newCollection;
            try
            {
                newCollection = await CreateCollection(dirName);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn("Failed to create the collection", ex);
                return null;
            }

            bool success = TryLoadCollection(newCollection);

            if (success)
            {
                CurrentCollection = newCollection;
                CreateCollectionVisibility = false;
                return newCollection;
            }

            return null;
        }

        /// <summary>
        /// Creates a new collection
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<CodexCollection> CreateCollection(string dirName)
        {
            if (!CodexCollectionOperations.IsLegalCollectionName(dirName, AllCodexCollections))
            {
                string msg = $"{dirName} is not a valid collection name";
                Logger.Warn(msg);
                throw new InvalidOperationException(msg);
            }

            CodexCollection newCollection = new CodexCollection(dirName);
            await _collectionStorageService.AllocateNewCollection(newCollection);

            AllCodexCollections.Add(newCollection);
            return newCollection;
        }

        // Rename Collection
        private RelayCommand<string>? _editCollectionNameCommand;
        public RelayCommand<string> EditCollectionNameCommand => _editCollectionNameCommand ??= new(
            EditCollectionName, 
            name => CodexCollectionOperations.IsLegalCollectionName(name, AllCodexCollections));
        public void EditCollectionName(string? newName)
        {
            if (!CodexCollectionOperations.IsLegalCollectionName(newName, AllCodexCollections)) return;
            
            CurrentCollection.RenameCollection(newName!);
            EditCollectionVisibility = false;
        }

        // Delete Collection
        private AsyncRelayCommand? _deleteCollectionCommand;
        public AsyncRelayCommand DeleteCollectionCommand => _deleteCollectionCommand ??= new(RaiseDeleteCollectionWarning);
        public async Task RaiseDeleteCollectionWarning()
        {
            if (CurrentCollection.AllCodices.Count > 0)
            {
                //"Are you Sure?"

                const string messageSingle = "There is still one item in this collection, if you don't want to remove it from COMPASS, move it to another collection first. Are you sure you want to continue?";
                string messageMultiple = $"There are still {CurrentCollection.AllCodices.Count} items in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";

                Notification areYouSure = Notification.AreYouSureNotification;
                areYouSure.Body = CurrentCollection.AllCodices.Count == 1 ? messageSingle : messageMultiple;

                var windowedNotificationService = ServiceResolver.Resolve<INotificationService>();
                await windowedNotificationService.ShowDialog(areYouSure);

                if (areYouSure.Result == NotificationAction.Confirm)
                {
                    DeleteCollection(CurrentCollection);
                }
            }
            else
            {
                DeleteCollection(CurrentCollection);
            }
        }
        public void DeleteCollection(CodexCollection toDelete)
        {
            //TODO dispose collection
            AllCodexCollections.Remove(toDelete);
            if (CurrentCollection == toDelete)
            {
                LoadInitialCollection();
            }

            _collectionStorageService.OnCollectionDeleted(toDelete);
        }

        //Export Collection
        private RelayCommand? _exportCommand;
        public RelayCommand ExportCommand => _exportCommand ??= new(Export);
        public void Export()
        {
            //open wizard
            ExportCollectionViewModel exportCollectionVM = new(CurrentCollection);
            ExportCollectionWizard wizard = new(exportCollectionVM);
            wizard.Show();
        }

        private AsyncRelayCommand? _exportTagsCommand;
        public AsyncRelayCommand ExportTagsCommand => _exportTagsCommand ??= new(_ => _collectionStorageService.ExportTags(CurrentCollection));

        //Import Collection
        private AsyncRelayCommand? _importCommand;
        public AsyncRelayCommand ImportCommand => _importCommand ??= new(ImportSatchelAsync);

        public async Task ImportSatchelAsync() => await ImportSatchelAsync(null);
        public async Task ImportSatchelAsync(string? path)
        {
            var collectionToImport = await _collectionStorageService.OpenSatchel(path);

            if (collectionToImport == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }

            //open wizard
            ImportCollectionViewModel importCollectionVM = new(collectionToImport);
            ImportCollectionWizard wizard = new(importCollectionVM);
            wizard.Show();
        }

        //Merge Collection into another
        private AsyncRelayCommand<string>? _mergeCollectionIntoCommand;
        public AsyncRelayCommand<string> MergeCollectionIntoCommand => _mergeCollectionIntoCommand ??= new(MergeIntoCollection);
        public async Task MergeIntoCollection(string? collectionToMergeInto)
        {
            if (string.IsNullOrEmpty(collectionToMergeInto) || !AllCodexCollections.Select(coll => coll.Name).Contains(collectionToMergeInto))
            {
                return;
            }

            //Are you sure?
            Notification areYouSure = Notification.AreYouSureNotification;
            areYouSure.Title = "Confirm merge";
            areYouSure.Body = $"You are about to merge '{CurrentCollection.Name}' into '{collectionToMergeInto}'. \n" +
                           $"This will copy all items, tags and preferences to the chosen collection. \n" +
                           $"Are you sure you want to continue?";

            await ServiceResolver.Resolve<INotificationService>().ShowDialog(areYouSure);

            if (areYouSure.Result != NotificationAction.Confirm) return;

            CodexCollection targetCollection = new(collectionToMergeInto);

            _collectionStorageService.Load(targetCollection);
            await targetCollection.MergeWith(CurrentCollection);

            Notification doneNotification = new("Merge Success", $"Successfully merged '{CurrentCollection.Name}' into '{collectionToMergeInto}'");
            
            //TODO toast notifications
            //await ServiceResolver.Resolve<INotificationService>().ShowToast(doneNotification);
        }
        #endregion
    }
}
