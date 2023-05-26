using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Properties;
using COMPASS.Tools;
using COMPASS.ViewModels.Import;
using COMPASS.Windows;
using Ionic.Zip;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.ViewModels
{
    public class CollectionViewModel : ObservableObject
    {
        public CollectionViewModel(MainViewModel mainViewModel)
        {
            MainVM = mainViewModel;
        }

        #region Properties
        public MainViewModel MainVM { get; init; }

        private CodexCollection _currentCollection;
        public CodexCollection CurrentCollection
        {
            get => _currentCollection;
            set
            {
                if (value == null) return;
                LoadCollection(value);
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<CodexCollection> _allCodexCollections = new();
        public ObservableCollection<CodexCollection> AllCodexCollections
        {
            get => _allCodexCollections;
            set => SetProperty(ref _allCodexCollections, value);
        }

        //Needed for binding to context menu "Move to Collection"
        public ObservableCollection<string> CollectionDirectories => new(AllCodexCollections.Select(collection => collection.DirectoryName));

        private FilterViewModel _filterVM;
        public FilterViewModel FilterVM
        {
            get => _filterVM;
            private set => SetProperty(ref _filterVM, value);
        }

        private TagsViewModel _tagsVM;
        public TagsViewModel TagsVM
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

        public bool IncludeFilesInExport { get; set; } = false;

        #endregion

        #region Methods and Commands
        public void LoadInitialCollection()
        {
            Directory.CreateDirectory(CodexCollection.CollectionsPath);

            //Get all collections by folder name
            AllCodexCollections = new(Directory
                .GetDirectories(CodexCollection.CollectionsPath)
                .Select(Path.GetFileName)
                .Where(IsLegalCollectionName)
                .Select(dir => new CodexCollection(dir)));

            while (CurrentCollection is null)
            {
                //in case of first boot or all saves are corrupted, create default collection
                if (AllCodexCollections.Count == 0)
                {
                    // if default collection already exists but is corrupted
                    // keep generating new collection names until a new one is found
                    bool created = false;
                    int attempt = 0;
                    string name = "Default Collection";
                    while (!created && attempt < 10) //only try 10 times to prevent infinite loop
                    {
                        if (!Path.Exists(Path.Combine(CodexCollection.CollectionsPath, name)))
                        {
                            CreateAndLoadCollection(name);
                            created = true;
                        }
                        else
                        {
                            name = $"Default Collection {attempt}";
                            attempt++;
                        }
                    }
                }

                //in case startup collection no longer exists, pick first one that does exists
                else if (AllCodexCollections.All(collection => collection.DirectoryName != Settings.Default.StartupCollection))
                {
                    Logger.Warn($"The collection {Settings.Default.StartupCollection} could not be found.", new DirectoryNotFoundException());
                    CurrentCollection = AllCodexCollections.First();
                    if (CurrentCollection is null)
                    {
                        // if it is null -> loading failed -> remove it from the pool and try again
                        AllCodexCollections.RemoveAt(0);
                    }
                }

                //otherwise, open startup collection
                else
                {
                    CurrentCollection = AllCodexCollections.First(collection => collection.DirectoryName == Settings.Default.StartupCollection);
                    if (CurrentCollection is null)
                    {
                        // if it is null -> loading failed -> remove it from the pool and try again
                        AllCodexCollections.Remove(AllCodexCollections.First(collection => collection.DirectoryName == Settings.Default.StartupCollection));
                    }
                }
            }
        }

        private bool IsLegalCollectionName(string dirName)
        {
            bool legal =
                dirName.IndexOfAny(Path.GetInvalidPathChars()) < 0
                && dirName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && AllCodexCollections.All(collection => collection.DirectoryName != dirName)
                && !String.IsNullOrWhiteSpace(dirName)
                && dirName.Length < 100
                && dirName[..2] != "__"; //reserved for protected folders
            return legal;
        }

        public void LoadCollection(CodexCollection collection)
        {
            _currentCollection?.Save();

            int success = collection.Load();
            if (success < 0)
            {
                string msg = success switch
                {
                    -1 => "The Tag data seems to be corrupted and could not be read.",
                    -2 => "The Codex data seems to be corrupted and could not be read.",
                    -3 => "Both the Tag and Codex data seems to be corrupted and could not be read.",
                    _ => ""
                };
                _ = MessageBox.Show($"Could not load {collection.DirectoryName}. \n" + msg, "Fail to Load Collection", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _currentCollection = collection;
            RaisePropertyChanged(nameof(CurrentCollection));
            MainVM?.CurrentLayout?.RaisePreferencesChanged();

            //create new viewmodels
            FilterVM = new(collection.AllCodices);
            TagsVM = new(this);

            _ = AutoImport();
        }

        public async Task AutoImport()
        {
            //Start Auto Imports
            ImportFolderViewModel folderImportVM = new()
            {
                FolderNames = CurrentCollection.Info.AutoImportDirectories.ToList(),
            };
            await Task.Delay(TimeSpan.FromSeconds(2));
            var toImport = folderImportVM.GetPathsFromFolders();
            ImportViewModel.ImportFiles(toImport);
        }

        public void Refresh()
        {
            LoadCollection(CurrentCollection);
            FilterVM.ReFilter(true);
        }

        private ActionCommand _toggleCreateCollectionCommand;
        public ActionCommand ToggleCreateCollectionCommand => _toggleCreateCollectionCommand ??= new(ToggleCreateCollection);
        private void ToggleCreateCollection() => CreateCollectionVisibility = !CreateCollectionVisibility;

        private ActionCommand _toggleEditCollectionCommand;
        public ActionCommand ToggleEditCollectionCommand => _toggleEditCollectionCommand ??= new(ToggleEditCollection);
        private void ToggleEditCollection() => EditCollectionVisibility = !EditCollectionVisibility;

        // Create CodexCollection
        private RelayCommand<string> _createCollectionCommand;
        public RelayCommand<string> CreateCollectionCommand => _createCollectionCommand ??= new(CreateAndLoadCollection, IsLegalCollectionName);
        public void CreateAndLoadCollection(string dirName)
        {
            var newCollection = CreateCollection(dirName);
            CurrentCollection = newCollection;
            CreateCollectionVisibility = false;
        }

        public CodexCollection CreateCollection(string dirName)
        {
            if (string.IsNullOrEmpty(dirName)) return null;
            CodexCollection newCollection = new(dirName);
            Directory.CreateDirectory(Path.Combine(newCollection.FullDataPath, "CoverArt"));
            Directory.CreateDirectory(Path.Combine(newCollection.FullDataPath, "Thumbnails"));
            AllCodexCollections.Add(newCollection);
            return newCollection;
        }

        // Rename Collection
        private RelayCommand<string> _editCollectionNameCommand;
        public RelayCommand<string> EditCollectionNameCommand => _editCollectionNameCommand ??= new(EditCollectionName, IsLegalCollectionName);
        public void EditCollectionName(string newName)
        {
            CurrentCollection.RenameCollection(newName);
            EditCollectionVisibility = false;
        }

        // Delete Collection
        private ActionCommand _deleteCollectionCommand;
        public ActionCommand DeleteCollectionCommand => _deleteCollectionCommand ??= new(RaiseDeleteCollectionWarning);
        public void RaiseDeleteCollectionWarning()
        {
            if (CurrentCollection.AllCodices.Count > 0)
            {
                //MessageBox "Are you Sure?"
                string sCaption = "Are you Sure?";

                const string messageSingle = "There is still one file in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";
                string messageMultiple = $"There are still {CurrentCollection.AllCodices.Count} files in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";

                string sMessageBoxText = CurrentCollection.AllCodices.Count == 1 ? messageSingle : messageMultiple;

                MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
                MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

                MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

                if (rsltMessageBox == MessageBoxResult.Yes)
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
            AllCodexCollections.Remove(CurrentCollection);
            CurrentCollection = AllCodexCollections.FirstOrDefault();

            //if Dir name of toDelete is empty, it will delete the entire collections folder
            if (String.IsNullOrEmpty(toDelete.DirectoryName)) return;
            Directory.Delete(toDelete.FullDataPath, true);
        }

        private ActionCommand _exportCommand;
        public ActionCommand ExportCommand => _exportCommand ??= new(async () => await Export());
        public async Task Export()
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = $"COMPASS File (*{Constants.COMPASSFileExtension})|*{Constants.COMPASSFileExtension}",
                FileName = CurrentCollection.DirectoryName,
                DefaultExt = Constants.COMPASSFileExtension
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                //make sure to save first
                MainViewModel.CollectionVM.CurrentCollection.Save();

                string targetPath = saveFileDialog.FileName;
                using ZipFile zip = new();
                zip.AddDirectory(CurrentCollection.FullDataPath);

                //copy everything to temp because codex paths it will be modified
                string tmpCollectionPath = Path.Combine(CodexCollection.CollectionsPath, $"__{CurrentCollection.DirectoryName}");
                Directory.CreateDirectory(tmpCollectionPath);
                string filesPath = Path.Combine(tmpCollectionPath, "Files");
                CodexCollection tmpCollection = new($"__{CurrentCollection.DirectoryName}");

                File.Copy(CurrentCollection.CodicesDataFilePath, tmpCollection.CodicesDataFilePath, true);
                tmpCollection.Load();

                var itemsWithOfflineSource = tmpCollection.AllCodices.Where(codex => codex.HasOfflineSource());
                string commonFolder = Utils.GetCommonFolder(itemsWithOfflineSource.Select(codex => codex.Path).ToList());
                foreach (Codex codex in itemsWithOfflineSource)
                {
                    string relativePath = codex.Path[commonFolder.Length..];
                    if (IncludeFilesInExport && File.Exists(codex.Path))
                    {
                        zip.AddFile(codex.Path, Path.Combine("Files", relativePath));
                    }
                    //strip longest common path so relative paths stay, given that full paths will break anyway
                    codex.Path = relativePath;
                }



                tmpCollection.SaveCodices();
                zip.UpdateFile(tmpCollection.CodicesDataFilePath, "");

                //Progress reporting
                var ProgressVM = ProgressViewModel.GetInstance();
                ProgressVM.Text = "Exporting Collection";
                ProgressVM.ShowCount = false;
                ProgressVM.ResetCounter();
                zip.SaveProgress += (object _, SaveProgressEventArgs args) =>
                {
                    ProgressVM.TotalAmount = Math.Max(ProgressVM.TotalAmount, args.EntriesTotal);
                    if (args.EventType == ZipProgressEventType.Saving_AfterWriteEntry)
                    {
                        ProgressVM.IncrementCounter();
                    }
                };

                //Export
                await Task.Run(() =>
                    {
                        zip.Save(targetPath);
                        if (IncludeFilesInExport) Directory.Delete(tmpCollectionPath, true);
                        ProgressVM.ShowCount = false;
                    });
                Logger.Info($"Exported {CurrentCollection.DirectoryName} to {targetPath}");
            }
        }

        private ActionCommand _exportTagsCommand;
        public ActionCommand ExportTagsCommand => _exportTagsCommand ??= new(ExportTags);
        public void ExportTags()
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = $"COMPASS File (*{Constants.COMPASSFileExtension})|*{Constants.COMPASSFileExtension}",
                FileName = $"{CurrentCollection.DirectoryName}_Tags",
                DefaultExt = Constants.COMPASSFileExtension
            };

            if (saveFileDialog.ShowDialog() != true) return;

            //make sure to save first
            CurrentCollection.SaveTags();

            string targetPath = saveFileDialog.FileName;
            using ZipFile zip = new();
            zip.AddFile(CurrentCollection.TagsDataFilePath, "");

            //Export
            zip.Save(targetPath);
            Logger.Info($"Exported Tags from {CurrentCollection.DirectoryName} to {targetPath}");
        }

        private ActionCommand _importCommand;
        public ActionCommand ImportCommand => _importCommand ??= new(Import);
        public void Import()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = $"COMPASS File (*{Constants.COMPASSFileExtension})|*{Constants.COMPASSFileExtension}",
                CheckFileExists = true,
                Multiselect = false,
                Title = "Choose a COMPASS file to import",
            };

            if (openFileDialog.ShowDialog() != true) return;

            ImportCollectionViewModel ImportCollectionVM = new(MainVM, openFileDialog.FileName);
            ImportCollectionWizard wizard = new(ImportCollectionVM);
            wizard.Show();
        }
        #endregion
    }
}
