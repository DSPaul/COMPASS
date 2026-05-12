using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Tools;
using System.Collections.ObjectModel;

namespace COMPASS.Common.ViewModels.Modals
{
    public class SettingsViewModel : ViewModelBase, IModalViewModel, IDisposable
    {
        private static readonly List<string> TabOrder = ["General", "Import", "Metadata", "Tools", "About"];

        public SettingsViewModel(string tabToOpen = "")
        {
            int tabIndex = TabOrder.FindIndex(t => t.Equals(tabToOpen, StringComparison.OrdinalIgnoreCase));
            SelectedTabIndex = tabIndex >= 0 ? tabIndex : 0;

            _applicationDataService = ServiceResolver.Resolve<IApplicationDataService>();
            _ioService = ServiceResolver.Resolve<IIOService>();
            _preferencesService = PreferencesService.GetInstance();
            

            SelectedCollectionVm = CollectionManager.CollectionVms.SingleOrDefault(vm => vm.Identifier == ActiveCollection.Name);

            if (SelectedCollectionVm == null)
            {
                Logger.Warn("The active collection was not found in the list of all known collections");
            }

            if (SelectedCollection != null)
            {
                BanishedPaths = new(SelectedCollection.Info.BanishedPaths.OrderBy(x => x));
                BanishedPaths.CollectionChanged += OnBanishedPathsChanged;
            }
            else
            {
                BanishedPaths = new ObservableCollection<string>();
            }
        }

        private readonly IApplicationDataService _applicationDataService;
        private readonly IIOService _ioService;
        private readonly PreferencesService _preferencesService;

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }
        
        
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

        private CodexCollection? SelectedCollection =>  SelectedCollectionVm?.Collection;

        #region IModalWindow

        public string WindowTitle => "Settings";
        public Action CloseAction { get; set; } = () => { };

        #endregion
        
        #region Load and Save Settings

        public Preferences Preferences => _preferencesService.Preferences;

        private void ApplyPreferences()
        {
            if (SelectedCollection == null) return;
            //Convert list back to dict because dict does not support two-way binding
            SelectedCollection.Info.FiletypePreferences = FiletypePreferences.ToDictionary(x => x.Key, x => x.Value);

            _preferencesService.SavePreferences();
        }

        public void Refresh()
        {
            if (SelectedCollection == null) return;
            
            //Tell the window that the FiletypePreferences dict might have changed so it needs to fetch it again
            _filetypePreferences = null;
            OnPropertyChanged(nameof(FiletypePreferences));

            SelectedCollection.Info.AutoImportFolders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(AutoImportFolders));
            SelectedCollection.Info.BanishedPaths.CollectionChanged += (_, _) => OnPropertyChanged(nameof(BanishedPaths));
        }
        #endregion

        #region Tab: General

        public bool PreferOnlineSource
        {
            get => _preferencesService.Preferences.OpenCodexPriority.First().Id == Preferences.ONLINE_SOURCE_PRIORITY_ID;
            set
            {
                if (value == PreferOnlineSource) return;
                //If the value changed, we can just switch the order around, because there are only 2 options
                _preferencesService.Preferences.OpenCodexPriority = new(_preferencesService.Preferences.OpenCodexPriority.Reverse());
                OnPropertyChanged();
            }
        }

        #endregion

        #region Tab: Import

        //Open folder in explorer
        private RelayCommand<string>? _showInExplorerCommand;
        public RelayCommand<string> ShowInExplorerCommand => _showInExplorerCommand ??= new(path =>
        {
            if (string.IsNullOrEmpty(path) || !Path.Exists(path)) return;
            _ioService.ShowInExplorer(path);
        });

        #region Auto import folders
        public ObservableCollection<Folder> AutoImportFolders => SelectedCollection != null ? 
            new(SelectedCollection.Info.AutoImportFolders.OrderBy(f => f.FullPath)) :
            [];

        //Edit a folder from auto import
        private AsyncRelayCommand<Folder>? _editAutoImportDirectoryCommand;
        public AsyncRelayCommand<Folder> EditAutoImportDirectoryCommand => _editAutoImportDirectoryCommand ??= new(EditAutoImportFolder);
        private async Task EditAutoImportFolder(Folder? folder)
        {
            if (folder is null) return;
            using var importFolderVM = new ImportFilesViewModel(autoImport: false);
            importFolderVM.ExistingFolders = [folder];
            await importFolderVM.Import();
            
            OnPropertyChanged(nameof(AutoImportFolders));
        }
        
        //Remove a folder from auto import
        private RelayCommand<Folder>? _removeAutoImportDirectoryCommand;
        public RelayCommand<Folder> RemoveAutoImportDirectoryCommand => _removeAutoImportDirectoryCommand ??= new(folder =>
            SelectedCollection!.Info.AutoImportFolders.Remove(folder!));
        
        //Add a directory from auto import
        private AsyncRelayCommand<string>? _addAutoImportDirectoryCommand;
        public AsyncRelayCommand<string> AddAutoImportDirectoryCommand => _addAutoImportDirectoryCommand ??= new(AddAutoImportDirectory);

        //Add a directory from auto import
        private AsyncRelayCommand? _pickAutoImportDirectoryCommand;
        public AsyncRelayCommand PickAutoImportDirectoryCommand => _pickAutoImportDirectoryCommand ??= new(PickAutoImportDirectory);

        private async Task PickAutoImportDirectory() => await AddAutoImportDirectory(await _ioService.PickFolder());
        private async Task AddAutoImportDirectory(string? dir)
        {
            if (!String.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            {
                using var importFolderVM = new ImportFilesViewModel(false);
                importFolderVM.RecursiveDirectories = [dir];
                await importFolderVM.Import();
            }
        }

        //File types to import
        private List<ObservableKeyValuePair<string, bool>>? _filetypePreferences;
        public List<ObservableKeyValuePair<string, bool>> FiletypePreferences
            => _filetypePreferences ??= SelectedCollection != null ? 
                SelectedCollection.Info.FiletypePreferences
                .Select(x => new ObservableKeyValuePair<string, bool>(x))
                .OrderBy(x => x.Key)
                .ToList() : 
                [];

        public ObservableCollection<string> BanishedPaths { get; }

        public void OnBanishedPathsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SelectedCollection?.Info.BanishedPaths.ReplaceRange(BanishedPaths);
        }
        
        #endregion

        #region Link Folders to Tag

        public bool AutoLinkFolderTagSameName
        {
            get => _preferencesService.Preferences.AutoLinkFolderTagSameName;
            set
            {
                _preferencesService.Preferences.AutoLinkFolderTagSameName = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #endregion

        #region Tab: Metadata
        public List<CodexProperty> MetaDataPreferences => Preferences.ImportableCodexProperties.OrderBy(x => x.Name).ToList();
        #endregion

        #region Tab: Data

        #region Manage Data

        #region Data Path 

        public string UserDataPath => _applicationDataService.UserDataPath;
        
        private AsyncRelayCommand? _changeDataPathCommand;
        public AsyncRelayCommand ChangeDataPathCommand => _changeDataPathCommand ??= new(ChooseNewDataPath);
        private async Task ChooseNewDataPath()
        {
            var folders = await ServiceResolver.Resolve<IFilesService>().OpenFoldersAsync(new()
            {
                Title = "Choose a new data location",
            });

            if (folders.Any())
            {
                var folder = folders.Single();
                string newPath = folder.Path.LocalPath;
                folder.Dispose();
                await _applicationDataService.UpdateUserDataPath(newPath);
                
                OnPropertyChanged(nameof(UserDataPath));
            }
        }

        private AsyncRelayCommand? _resetDataPathCommand;
        public AsyncRelayCommand ResetDataPathCommand => _resetDataPathCommand ??= new(ResetDataPath);
        private async Task ResetDataPath()
        {
            await _applicationDataService.ResetUserDataPath();
            OnPropertyChanged(nameof(UserDataPath));
        }
        #endregion

        private RelayCommand? _browseLocalFilesCommand;
        public RelayCommand BrowseLocalFilesCommand => _browseLocalFilesCommand ??= new(BrowseLocalFiles);
        public void BrowseLocalFiles() => _ioService.ShowInExplorer(_applicationDataService.UserDataPath);
        
        #endregion

        #endregion

        #region Tab: Tools
        public ToolsViewModel ToolsVM { get; } = new();
        #endregion
        //for debugging only
        public void RegenAllThumbnails()
        {
            foreach (Codex codex in ActiveCollection.AllCodices)
            {
                //codex.Thumbnail = codex.CoverArt.Replace("CoverArt", "Thumbnails");
                using var thumbnail = CoverService.CreateThumbnail(codex);
                codex.NotifyCoverChanged();
            }
        }
        
        #region Tab: About
        public string Version => "Version: " + ApplicationService.GetVersion();

        private AsyncRelayCommand? _checkForUpdatesCommand;
        public AsyncRelayCommand CheckForUpdatesCommand => _checkForUpdatesCommand ??= new(UpdateManager.CheckForUpdates);
        #endregion

        public void Dispose()
        {
            BanishedPaths.CollectionChanged -= OnBanishedPathsChanged;
            _selectedCollectionHandle?.Dispose();
            ToolsVM.Dispose();
        }
    }
}
