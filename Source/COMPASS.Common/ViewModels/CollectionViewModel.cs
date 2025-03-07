﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.SidePanels;
using COMPASS.Common.Views.Windows;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

namespace COMPASS.Common.ViewModels
{
    public class CollectionViewModel : ViewModelBase
    {
        public CollectionViewModel(MainViewModel? mainViewModel)
        {
            MainVM = mainViewModel;
            _preferencesService = PreferencesService.GetInstance();
            _environmentVarsService = App.Container.Resolve<IEnvironmentVarsService>();

            //only to avoid null references, should be overwritten as soon as the UI loads, which calls refresh
            _filterVM = new([]);
            _tagsVM = new(new("__tmp"), _filterVM);

            //Create Collections Directory
            try
            {
                Directory.CreateDirectory(CodexCollection.CollectionsPath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create folder to store user data, so data cannot be saved", ex);
                string msg = $"Failed to create a folder to store user data at {_environmentVarsService.CompassDataPath}, " +
                             $"please pick a new location to save your data. Creation failed with the following error {ex.Message}";
                IOService.AskNewCodexFilePath(msg).Wait();
            }

            try
            {
                //Get all collections by folder name
                AllCodexCollections = new(Directory
                    .GetDirectories(CodexCollection.CollectionsPath)
                    .Select(dir => Path.GetFileName(dir))
                    .Where(IsLegalCollectionName)
                    .Select(dir => new CodexCollection(dir)));
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to find existing collections in {CodexCollection.CollectionsPath}", ex);
                AllCodexCollections = [];
            }

            LoadInitialCollection();

            Debug.Assert(_currentCollection is not null, "Current Collection should never be null after loading Initial Collection");
        }

        private readonly PreferencesService _preferencesService;
        private readonly IEnvironmentVarsService _environmentVarsService;

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
                _currentCollection?.Save();
                SetProperty(ref _currentCollection!, value);
            }
        }

        private ObservableCollection<CodexCollection> _allCodexCollections = [];
        public ObservableCollection<CodexCollection> AllCodexCollections
        {
            get => _allCodexCollections;
            init => SetProperty(ref _allCodexCollections, value);
        }

        //Needed for binding to context menu "Move to Collection"
        public ObservableCollection<string> CollectionDirectories => new(AllCodexCollections.Select(collection => collection.DirectoryName));

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
            while (!Directory.Exists(CodexCollection.CollectionsPath))
            {
                try
                {
                    Directory.CreateDirectory(CodexCollection.CollectionsPath);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to create folder to store user data, so data cannot be saved", ex);
                    string msg = $"Failed to create a folder to store user data at {_environmentVarsService.CompassDataPath}, " +
                                 $"please pick a new location to save your data. Creation failed with the following error {ex.Message}";
                    IOService.AskNewCodexFilePath(msg).Wait();
                }
            }

            bool initSuccess = false;
            while (!initSuccess)
            {
                //in case of first boot, or if the existing ones couldn't load, create default collection
                if (AllCodexCollections.Count == 0)
                {
                    string name = "Default Collection";
                    var createdCollection = CreateAndLoadCollection(name);
                    if (createdCollection != null)
                    {
                        initSuccess = true;
                    }
                    else
                    {
                        //If no collections are found and creation fails, we are stuck in an infinite loop which is bad so throw and crash
                        throw new IOException($"Could not create the default collection at {Path.Combine(CodexCollection.CollectionsPath, name)}");
                    }
                }

                //otherwise, open startup collection
                else if (AllCodexCollections.Any(collection => collection.DirectoryName == _preferencesService.Preferences.UIState.StartupCollection))
                {
                    var startupCollection = AllCodexCollections.First(collection => collection.DirectoryName == _preferencesService.Preferences.UIState.StartupCollection);
                    initSuccess = TryLoadCollection(startupCollection);
                    if (!initSuccess)
                    {
                        // if loading failed -> remove it from the pool and try again
                        AllCodexCollections.Remove(startupCollection);
                    }
                }

                //in case startup collection no longer exists, pick first one that does exists
                else
                {
                    Logger.Warn($"The collection {_preferencesService.Preferences.UIState.StartupCollection} could not be found.", new DirectoryNotFoundException());
                    var firstCollection = AllCodexCollections.First();
                    initSuccess = TryLoadCollection(firstCollection);
                    if (!initSuccess)
                    {
                        // if loading failed -> remove it from the pool and try again
                        AllCodexCollections.RemoveAt(0);
                    }
                }


            }
        }

        public bool IsLegalCollectionName(string? dirName)
        {
            bool legal =
                !String.IsNullOrWhiteSpace(dirName)
                && dirName.IndexOfAny(Path.GetInvalidPathChars()) < 0
                && dirName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && AllCodexCollections.All(collection => collection.DirectoryName != dirName)
                && dirName.Length < 100
                && (dirName.Length < 2 || dirName[..2] != "__"); //reserved for protected folders
            return legal;
        }

        /// <summary>
        /// Tries to load a collection and will set <see cref="CurrentCollection"/> if succesfull
        /// </summary>
        /// <param name="collection"></param>
        /// <returns>A boolean indiciating whether or not the load was a succes </returns>
        private bool TryLoadCollection(CodexCollection collection)
        {
            int success = collection.Load();
            if (success < 0)
            {
                string msg = success switch
                {
                    -1 => "The save file for the Tags seems to be corrupted and could not be read.",
                    -2 => "The save file with all items seems to be corrupted and could not be read.",
                    -3 => "Both the save files with tags and items seem to be corrupted and could not be read.",
                    _ => ""
                };
                Notification error = new("Failed to Load Collection", $"Could not load {collection.DirectoryName}. \n" + msg, Severity.Error);
                App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed).Show(error);
                return false;
            }

            CurrentCollection = collection;
            return true;
        }

        public async Task AutoImport()
        {
            //Start Auto Imports
            ImportFolderViewModel folderImportVM = new(manuallyTriggered: false)
            {
                NonRecursiveDirectories = CurrentCollection?.Info.AutoImportFolders.Flatten().Select(f => f.FullPath).ToList() ?? [],
            };
            await Task.Delay(TimeSpan.FromSeconds(2));
            await folderImportVM.Import();
        }

        public async Task Refresh()
        {
            CurrentCollection.Save();
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
        private RelayCommand<string>? _createCollectionCommand;
        public RelayCommand<string> CreateCollectionCommand =>
            _createCollectionCommand ??= new(name => CreateAndLoadCollection(name), IsLegalCollectionName);
        public CodexCollection? CreateAndLoadCollection(string? dirName)
        {
            if (dirName == null)
            {
                return null;
            }

            CodexCollection newCollection;
            try
            {
                newCollection = CreateCollection(dirName);
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
        public CodexCollection CreateCollection(string dirName)
        {
            if (!IsLegalCollectionName(dirName))
            {
                string msg = $"{dirName} is not a valid collection name";
                Logger.Warn(msg);
                throw new InvalidOperationException(msg);
            }

            CodexCollection newCollection = new(dirName);

            newCollection.InitAsNew();

            AllCodexCollections.Add(newCollection);
            return newCollection;
        }

        // Rename Collection
        private RelayCommand<string>? _editCollectionNameCommand;
        public RelayCommand<string> EditCollectionNameCommand => _editCollectionNameCommand ??= new(EditCollectionName, IsLegalCollectionName);
        public void EditCollectionName(string? newName)
        {
            if (!IsLegalCollectionName(newName)) return;
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

                var windowedNotificationService = App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed);
                await windowedNotificationService.Show(areYouSure);

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
            AllCodexCollections.Remove(toDelete);
            if (CurrentCollection == toDelete)
            {
                LoadInitialCollection();
            }

            //if Dir name of toDelete is empty, it will delete the entire collections folder
            if (String.IsNullOrEmpty(toDelete.DirectoryName)) return;
            if (Directory.Exists(toDelete.FullDataPath)) //does not exist if collection was never saved
            {
                Directory.Delete(toDelete.FullDataPath, true);
            }
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
        public AsyncRelayCommand ExportTagsCommand => _exportTagsCommand ??= new(ExportTags);
        public async Task ExportTags()
        {
            var filesService = App.Container.Resolve<IFilesService>();
            var savedFile = await filesService.SaveFileAsync(new()
            {
                FileTypeChoices = [filesService.SatchelExtensionFilter],
                SuggestedFileName = $"{CurrentCollection.DirectoryName}_Tags",
                DefaultExtension = Constants.SatchelExtension,
            });

            if (savedFile == null) return;

            //make sure to save first
            CurrentCollection.SaveTags();

            string targetPath = savedFile.Path.AbsolutePath;
            savedFile.Dispose();
            using var archive = ZipArchive.Create();
            archive.AddEntry(Constants.TagsFileName, CurrentCollection.TagsDataFilePath);

            //Export
            archive.SaveTo(targetPath, CompressionType.None);
            Logger.Info($"Exported Tags from {CurrentCollection.DirectoryName} to {targetPath}");
        }

        //Import Collection
        private AsyncRelayCommand? _importCommand;
        public AsyncRelayCommand ImportCommand => _importCommand ??= new(ImportSatchelAsync);

        public async Task ImportSatchelAsync() => await ImportSatchelAsync(null);
        public async Task ImportSatchelAsync(string? path)
        {
            var collectionToImport = await IOService.OpenSatchel(path);

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
            if (String.IsNullOrEmpty(collectionToMergeInto) || !AllCodexCollections.Select(coll => coll.DirectoryName).Contains(collectionToMergeInto))
            {
                return;
            }

            //Are you sure?
            Notification areYouSure = Notification.AreYouSureNotification;
            areYouSure.Title = "Confirm merge";
            areYouSure.Body = $"You are about to merge '{CurrentCollection.DirectoryName}' into '{collectionToMergeInto}'. \n" +
                           $"This will copy all items, tags and preferences to the chosen collection. \n" +
                           $"Are you sure you want to continue?";

            await App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed).Show(areYouSure);

            if (areYouSure.Result != NotificationAction.Confirm) return;

            CodexCollection targetCollection = new(collectionToMergeInto);

            targetCollection.Load(makeStartupCollection: false);
            await targetCollection.MergeWith(CurrentCollection);

            Notification doneNotification = new("Merge Success", $"Successfully merged '{CurrentCollection.DirectoryName}' into '{collectionToMergeInto}'");
            await App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Toast).Show(doneNotification);
        }
        #endregion
    }
}
