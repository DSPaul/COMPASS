using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.Views.Windows;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;

namespace COMPASS.Common.ViewModels.Modals
{
    public class SettingsViewModel : ViewModelBase, IModalViewModel
    {
        public SettingsViewModel(string tabToOpen = "")
        {
            //TODO use tabToOpen 
            
            _preferencesService = PreferencesService.GetInstance();
            _environmentVarsService = ServiceResolver.Resolve<IEnvironmentVarsService>();
            _collectionStorageService = ServiceResolver.Resolve<ICodexCollectionStorageService>();
            _applicationDataService = ServiceResolver.Resolve<IApplicationDataService>();

            WebViewDataDir = Path.Combine(_environmentVarsService.CompassDataPath, "WebViewData");

            BanishedPaths = new(MainViewModel.CollectionVM.CurrentCollection.Info.BanishedPaths.OrderBy(x => x));
            BanishedPaths.CollectionChanged += OnBanishedPathsChanged;
        }

        private readonly PreferencesService _preferencesService;
        private readonly IEnvironmentVarsService _environmentVarsService;
        private readonly ICodexCollectionStorageService _collectionStorageService;
        private readonly IApplicationDataService _applicationDataService;

        #region IModalWindow

        public string WindowTitle => "Settings";
        public int? WindowWidth { get; } = 1000;
        public int? WindowHeight { get; } = 500;

        public SizeToContent SizeToContent { get; } = SizeToContent.Manual;

        public Action CloseAction { get; set; } = () => { };

        #endregion
        
        #region Load and Save Settings

        public Preferences Preferences => _preferencesService.Preferences;

        private void ApplyPreferences()
        {
            //Convert list back to dict because dict does not support two-way binding
            MainViewModel.CollectionVM.CurrentCollection.Info.FiletypePreferences = FiletypePreferences.ToDictionary(x => x.Key, x => x.Value);

            _preferencesService.SavePreferences();
        }

        public void Refresh()
        {
            //Tell the window that the FiletypePreferences dict might have changed so it needs to fetch it again
            _filetypePreferences = null;
            OnPropertyChanged(nameof(FiletypePreferences));

            MainViewModel.CollectionVM.CurrentCollection.Info.AutoImportFolders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(AutoImportFolders));
            MainViewModel.CollectionVM.CurrentCollection.Info.BanishedPaths.CollectionChanged += (_, _) => OnPropertyChanged(nameof(BanishedPaths));
        }
        #endregion

        #region Tab: General

        public bool PreferOnlineSource
        {
            get => _preferencesService.Preferences.OpenCodexPriority.First().ID == Preferences.ONLINE_SOURCE_PRIORITY_ID;
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
        public ObservableCollection<Folder> AutoImportFolders => new(MainViewModel.CollectionVM.CurrentCollection.Info.AutoImportFolders.OrderBy(f => f.FullPath));

        //Edit a folder from auto import
        private AsyncRelayCommand<Folder>? _editAutoImportDirectoryCommand;
        public AsyncRelayCommand<Folder> EditAutoImportDirectoryCommand => _editAutoImportDirectoryCommand ??= new(EditAutoImportFolder);
        private async Task EditAutoImportFolder(Folder? folder)
        {
            if (folder is null) return;
            var importFolderVM = new ImportFilesViewModel(autoImport: false)
            {
                ExistingFolders = [folder],
            };
            await importFolderVM.Import();
            
            OnPropertyChanged(nameof(AutoImportFolders));
        }
        
        //Remove a folder from auto import
        private RelayCommand<Folder>? _removeAutoImportDirectoryCommand;
        public RelayCommand<Folder> RemoveAutoImportDirectoryCommand => _removeAutoImportDirectoryCommand ??= new(folder =>
            MainViewModel.CollectionVM.CurrentCollection.Info.AutoImportFolders.Remove(folder!));
        
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
                var importFolderVM = new ImportFilesViewModel(false)
                {
                    RecursiveDirectories = [dir],
                };
                await importFolderVM.Import();
            }
        }

        //File types to import
        private List<ObservableKeyValuePair<string, bool>>? _filetypePreferences;
        public List<ObservableKeyValuePair<string, bool>> FiletypePreferences
            => _filetypePreferences
            ??= MainViewModel.CollectionVM.CurrentCollection.Info.FiletypePreferences
                .Select(x => new ObservableKeyValuePair<string, bool>(x))
                .OrderBy(x => x.Key)
                .ToList();

        public ObservableCollection<string> BanishedPaths { get; }

        public void OnBanishedPathsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            MainViewModel.CollectionVM.CurrentCollection.Info.BanishedPaths.Clear();
            MainViewModel.CollectionVM.CurrentCollection.Info.BanishedPaths.AddRange(BanishedPaths);
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
        public List<CodexProperty> MetaDataPreferences => Preferences.CodexProperties.OrderBy(x => x.Name).ToList();
        #endregion

        #region Tab: Data

        #region Fix Broken refs

        public IEnumerable<Codex> BrokenCodices => MainViewModel.CollectionVM.CurrentCollection.AllCodices
               .Where(codex => codex.Sources.HasOfflineSource()) //do this check so message doesn't count codices that never had a path to begin with
               .Where(codex => !Path.Exists(codex.Sources.Path));
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
        private void ShowBrokenCodices() => MainViewModel.CollectionVM.FilterVM.AddFilter(new HasBrokenPathFilter());

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

        public string RenameCompleteMessage => $"Renamed Path Reference in {AmountRenamed} items.";

        private RelayCommand<object[]>? _renameFolderRefCommand;
        public RelayCommand<object[]> RenameFolderRefCommand => _renameFolderRefCommand ??= new(RenameFolderReferences);

        private void RenameFolderReferences(object[]? args)
        {
            if (args is null || args.Length != 2) { return; }
            RenameFolderReferences(args[0] as string, args[1] as string);
        }

        private void RenameFolderReferences(string? oldPath, string? newPath)
        {
            if (String.IsNullOrWhiteSpace(oldPath) || newPath is null) return;

            AmountRenamed = 0;
            foreach (Codex codex in MainViewModel.CollectionVM.CurrentCollection.AllCodices)
            {
                if (codex.Sources.HasOfflineSource() && codex.Sources.Path.Contains(oldPath))
                {
                    string updatedPath = codex.Sources.Path.Replace(oldPath, newPath);
                    //only replace path if old one was broken and new one exists
                    if (!File.Exists(codex.Sources.Path) && File.Exists(updatedPath))
                    {
                        codex.Sources.Path = updatedPath;
                        AmountRenamed++;
                    }
                }
            }
            _collectionStorageService.SaveCodices(MainViewModel.CollectionVM.CurrentCollection);
        }

        //remove refs from codices
        private RelayCommand? _removeBrokenRefsCommand;
        public RelayCommand RemoveBrokenRefsCommand => _removeBrokenRefsCommand ??= new(RemoveBrokenReferences);
        public void RemoveBrokenReferences()
        {
            foreach (Codex codex in BrokenCodices)
            {
                codex.Sources.Path = "";
            }
            BrokenCodicesChanged();
            _collectionStorageService.SaveCodices(MainViewModel.CollectionVM.CurrentCollection);
        }

        //Remove Codices with broken refs
        private AsyncRelayCommand? _deleteCodicesWithBrokenRefsCommand;
        public AsyncRelayCommand DeleteCodicesWithBrokenRefsCommand => _deleteCodicesWithBrokenRefsCommand ??= new(RemoveCodicesWithBrokenRefs);
        public async Task RemoveCodicesWithBrokenRefs()
        {
            await CodexOperations.DeleteCodices(BrokenCodices.ToList(), true);
            BrokenCodicesChanged();
        }
        #endregion

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

        private LoadingWindow? _lw;

        private AsyncRelayCommand? _backupLocalFilesCommand;
        public AsyncRelayCommand BackupLocalFilesCommand => _backupLocalFilesCommand ??= new(BackupLocalFiles);
        public async Task BackupLocalFiles()
        {
            var filesService = ServiceResolver.Resolve<IFilesService>();
            var saveFile = await filesService.SaveFileAsync(new()
            {
                FileTypeChoices = [filesService.ZipExtensionFilter]
            });

            if (saveFile != null)
            {
                string targetPath = saveFile.Path.AbsolutePath;
                saveFile.Dispose();
                _lw = new("Compressing to Zip File");
                _lw.Show();

                //save first
                ApplyPreferences();
                _collectionStorageService.Save(MainViewModel.CollectionVM.CurrentCollection);

                await Task.Run(() =>  _collectionStorageService.CompressUserDataToZip(targetPath));

                _lw.Close();
            }
        }

        private AsyncRelayCommand? _restoreBackupCommand;
        public AsyncRelayCommand RestoreBackupCommand => _restoreBackupCommand ??= new(RestoreBackup);
        public async Task RestoreBackup()
        {
            var filesService = ServiceResolver.Resolve<IFilesService>();
            var files = await filesService.OpenFilesAsync(new()
            {
                FileTypeFilter = [filesService.ZipExtensionFilter]
            });

            if (files.Any())
            {
                using var file = files.Single();
                string targetPath = file.Path.AbsolutePath;
                _lw = new("Restoring Backup");
                _lw.Show();

                await Task.Run(() => ExtractZip(targetPath));

                //restore collection that was open
                MainViewModel.CollectionVM.CurrentCollection = new(_preferencesService.Preferences.UIState.StartupCollection);
                _lw?.Close();
            }
        }

        private void ExtractZip(string sourcePath)
        {
            if (!Path.Exists(sourcePath))
            {
                Logger.Warn($"Cannot extract sourcePath as it does not exit");
                return;
            }
            using ZipArchive archive = ZipArchive.Open(sourcePath);
            archive.ExtractToDirectory(_environmentVarsService.CompassDataPath);
        }
        #endregion

        #endregion

        //for debugging only
        public void RegenAllThumbnails()
        {
            foreach (Codex codex in MainViewModel.CollectionVM.CurrentCollection.AllCodices)
            {
                //codex.Thumbnail = codex.CoverArt.Replace("CoverArt", "Thumbnails");
                CoverService.CreateThumbnail(codex);
                codex.RefreshThumbnail();
            }
        }

        #region Tab: What's New

        public readonly string WebViewDataDir;

        #endregion

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
    }
}
