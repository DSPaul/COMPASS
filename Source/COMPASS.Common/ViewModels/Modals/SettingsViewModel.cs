using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.ViewModels.Modals
{
    public class SettingsViewModel : ViewModelBase, IModalViewModel, IDisposable
    {
        //TODO dispose handle on close
        public SettingsViewModel(string tabToOpen = "")
        {
            //TODO use tabToOpen 
            
            _preferencesService = PreferencesService.GetInstance();
            _environmentVarsService = ServiceResolver.Resolve<IEnvironmentVarsService>();
            _applicationDataService = ServiceResolver.Resolve<IApplicationDataService>();
            

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

        private readonly PreferencesService _preferencesService;
        private readonly IEnvironmentVarsService _environmentVarsService;
        private readonly IApplicationDataService _applicationDataService;
        
        
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

        //TODO check if this virtualization stuff is still relevant
        //#region Virtualization Preferences

        //public bool DoVirtualizationList
        //{
        //    get => Properties.Settings.Default.DoVirtualizationList;
        //    set
        //    {
        //        Properties.Settings.Default.DoVirtualizationList = value;
        //        MVM?.CurrentLayout.UpdateDoVirtualization();
        //    }
        //}

        //public bool DoVirtualizationCard
        //{
        //    get => Properties.Settings.Default.DoVirtualizationCard;
        //    set
        //    {
        //        Properties.Settings.Default.DoVirtualizationCard = value;
        //        MVM?.CurrentLayout.UpdateDoVirtualization();
        //    }
        //}

        //public bool DoVirtualizationTile
        //{
        //    get => Properties.Settings.Default.DoVirtualizationTile;
        //    set
        //    {
        //        Properties.Settings.Default.DoVirtualizationTile = value;
        //        MVM?.CurrentLayout.UpdateDoVirtualization();
        //    }
        //}

        //public int VirtualizationThresholdList
        //{
        //    get => Properties.Settings.Default.VirtualizationThresholdList;
        //    set
        //    {
        //        Properties.Settings.Default.VirtualizationThresholdList = value;
        //        MVM?.CurrentLayout.UpdateDoVirtualization();
        //    }
        //}

        //public int VirtualizationThresholdCard
        //{
        //    get => Properties.Settings.Default.VirtualizationThresholdCard;
        //    set
        //    {
        //        Properties.Settings.Default.VirtualizationThresholdCard = value;
        //        MVM?.CurrentLayout.UpdateDoVirtualization();
        //    }
        //}

        //public int VirtualizationThresholdTile
        //{
        //    get => Properties.Settings.Default.VirtualizationThresholdTile;
        //    set
        //    {
        //        Properties.Settings.Default.VirtualizationThresholdTile = value;
        //        MVM?.CurrentLayout.UpdateDoVirtualization();
        //    }
        //}
        //#endregion

        #endregion

        #region Tab: Import

        //Open folder in explorer
        private RelayCommand<string>? _showInExplorerCommand;
        public RelayCommand<string> ShowInExplorerCommand => _showInExplorerCommand ??= new(path =>
        {
            if (string.IsNullOrEmpty(path) || !Path.Exists(path)) return;
            IOService.ShowInExplorer(path);
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

        private async Task PickAutoImportDirectory() => await AddAutoImportDirectory(await IOService.PickFolder().ConfigureAwait(false));
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
            SelectedCollection?.Info.BanishedPaths.Clear();
            SelectedCollection?.Info.BanishedPaths.AddRange(BanishedPaths);
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

        public string CompassDataPath => _environmentVarsService.CompassDataPath;
        
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
                string newPath = folder.Path.AbsolutePath;
                folder.Dispose();
                await _applicationDataService.UpdateRootDirectory(newPath);
                
                OnPropertyChanged(nameof(CompassDataPath));
            }
        }

        private AsyncRelayCommand? _resetDataPathCommand;
        public AsyncRelayCommand ResetDataPathCommand => _resetDataPathCommand ??= new(ResetDataPath);
        private async Task ResetDataPath()
        {
            await _applicationDataService.UpdateRootDirectory(IEnvironmentVarsService.DefaultDataPath);
            OnPropertyChanged(nameof(CompassDataPath));
        }
        #endregion

        private RelayCommand? _browseLocalFilesCommand;
        public RelayCommand BrowseLocalFilesCommand => _browseLocalFilesCommand ??= new(BrowseLocalFiles);
        public void BrowseLocalFiles()
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = _environmentVarsService.CompassDataPath,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }

        
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
                CoverService.CreateThumbnail(codex);
                codex.RefreshThumbnail();
            }
        }
        
        #region Tab: About
        public string Version => "Version: " + Assembly.GetExecutingAssembly().GetName().Version?.ToString()[0..5];

        private RelayCommand? _checkForUpdatesCommand;
        public RelayCommand CheckForUpdatesCommand => _checkForUpdatesCommand ??= new(CheckForUpdates);
        private void CheckForUpdates()
        {
            //TODO
            //AutoUpdater.Mandatory = true;
            //AutoUpdater.Start();
        }
        #endregion

        public void Dispose()
        {
            BanishedPaths.CollectionChanged -= OnBanishedPathsChanged;
            _selectedCollectionHandle?.Dispose();
            ToolsVM.Dispose();
        }
    }
}
