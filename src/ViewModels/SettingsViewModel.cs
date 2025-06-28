﻿using Autofac;
using AutoUpdaterDotNET;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Interfaces;
using COMPASS.Models;
using COMPASS.Models.CodexProperties;
using COMPASS.Models.Enums;
using COMPASS.Models.Filters;
using COMPASS.Models.Preferences;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.ViewModels.Import;
using COMPASS.Windows;
using Microsoft.Win32;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace COMPASS.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private SettingsViewModel()
        {
            _preferencesService = PreferencesService.GetInstance();

            //Save the compass data path into an env var, needed for cross platform transition
            EnvironmentVarsService.CompassDataPath = Properties.Settings.Default.CompassDataPath;
        }

        #region singleton pattern
        private static SettingsViewModel? _settingsVM;
        public static SettingsViewModel GetInstance() => _settingsVM ??= new SettingsViewModel();
        #endregion

        PreferencesService _preferencesService;

        public MainViewModel? MVM { get; set; }

        #region Load and Save Settings

        public Preferences Preferences => _preferencesService.Preferences;

        public void ApplyPreferences()
        {
            //Convert list back to dict because dict does not support two way binding
            MainViewModel.CollectionVM.CurrentCollection.Info.FiletypePreferences = FiletypePreferences.ToDictionary(x => x.Key, x => x.Value);

            _preferencesService.SavePreferences();
        }

        public void Refresh()
        {
            //Tell the window that the FiletypePreferences dict might have changed so it needs to fetch it again
            _filetypePreferences = null;
            OnPropertyChanged(nameof(FiletypePreferences));
        }
        #endregion

        #region Tab: Preferences

        #region Virtualization Preferences

        public bool DoVirtualizationList
        {
            get => Properties.Settings.Default.DoVirtualizationList;
            set
            {
                Properties.Settings.Default.DoVirtualizationList = value;
                MVM?.CurrentLayout.UpdateDoVirtualization();
            }
        }

        public bool DoVirtualizationCard
        {
            get => Properties.Settings.Default.DoVirtualizationCard;
            set
            {
                Properties.Settings.Default.DoVirtualizationCard = value;
                MVM?.CurrentLayout.UpdateDoVirtualization();
            }
        }

        public bool DoVirtualizationTile
        {
            get => Properties.Settings.Default.DoVirtualizationTile;
            set
            {
                Properties.Settings.Default.DoVirtualizationTile = value;
                MVM?.CurrentLayout.UpdateDoVirtualization();
            }
        }

        public int VirtualizationThresholdList
        {
            get => Properties.Settings.Default.VirtualizationThresholdList;
            set
            {
                Properties.Settings.Default.VirtualizationThresholdList = value;
                MVM?.CurrentLayout.UpdateDoVirtualization();
            }
        }

        public int VirtualizationThresholdCard
        {
            get => Properties.Settings.Default.VirtualizationThresholdCard;
            set
            {
                Properties.Settings.Default.VirtualizationThresholdCard = value;
                MVM?.CurrentLayout.UpdateDoVirtualization();
            }
        }

        public int VirtualizationThresholdTile
        {
            get => Properties.Settings.Default.VirtualizationThresholdTile;
            set
            {
                Properties.Settings.Default.VirtualizationThresholdTile = value;
                MVM?.CurrentLayout.UpdateDoVirtualization();
            }
        }
        #endregion

        #endregion

        #region Tab: Import

        //Open folder in explorer
        private RelayCommand<string>? _showInExplorerCommand;
        public RelayCommand<string> ShowInExplorerCommand => _showInExplorerCommand ??= new(path =>
        {
            if (String.IsNullOrEmpty(path) || !Path.Exists(path)) return;
            IOService.ShowInExplorer(path);
        });

        #region Auto import folders
        public CollectionViewSource AutoImportFoldersViewSource
        {
            get
            {
                CollectionViewSource temp = new()
                {
                    Source = MainViewModel.CollectionVM.CurrentCollection.Info.AutoImportFolders,
                    IsLiveSortingRequested = true,
                };
                temp.SortDescriptions.Add(new SortDescription("FullPath", ListSortDirection.Ascending));
                return temp;
            }
        }

        //Edit a folder from auto import
        private RelayCommand<Folder>? _removeAutoImportDirectoryCommand;
        public RelayCommand<Folder> RemoveAutoImportDirectoryCommand => _removeAutoImportDirectoryCommand ??= new(folder =>
            MainViewModel.CollectionVM.CurrentCollection.Info.AutoImportFolders.Remove(folder!));

        //Remove a folder from auto import
        private AsyncRelayCommand<Folder>? _editAutoImportDirectoryCommand;
        public AsyncRelayCommand<Folder> EditAutoImportDirectoryCommand => _editAutoImportDirectoryCommand ??= new(EditAutoImportFolder);

        //Add a directory from auto import
        private AsyncRelayCommand<string>? _addAutoImportDirectoryCommand;
        public AsyncRelayCommand<string> AddAutoImportDirectoryCommand => _addAutoImportDirectoryCommand ??= new(AddAutoImportDirectory);

        //Add a directory from auto import
        private AsyncRelayCommand? _pickAutoImportDirectoryCommand;
        public AsyncRelayCommand PickAutoImportDirectoryCommand => _pickAutoImportDirectoryCommand ??= new(PickAutoImportDirectory);

        private async Task PickAutoImportDirectory() => await AddAutoImportDirectory(IOService.PickFolder());
        private async Task AddAutoImportDirectory(string? dir)
        {
            if (!String.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            {
                var importFolderVM = new ImportFolderViewModel(true)
                {
                    RecursiveDirectories = new List<string> { dir },
                };
                await importFolderVM.Import();
            }
        }

        private async Task EditAutoImportFolder(Folder? folder)
        {
            if (folder is null) return;
            var importFolderVM = new ImportFolderViewModel(true)
            {
                ExistingFolders = new List<Folder> { folder },
            };
            await importFolderVM.Import();
        }

        //File types to import
        private List<ObservableKeyValuePair<string, bool>>? _filetypePreferences;
        public List<ObservableKeyValuePair<string, bool>> FiletypePreferences
            => _filetypePreferences
            ??= MainViewModel.CollectionVM.CurrentCollection.Info.FiletypePreferences.Select(x => new ObservableKeyValuePair<string, bool>(x)).ToList();

        public CollectionViewSource BanishedPaths
        {
            get
            {
                CollectionViewSource temp = new()
                {
                    Source = MainViewModel.CollectionVM.CurrentCollection.Info.BanishedPaths,
                    IsLiveSortingRequested = true,
                };
                temp.SortDescriptions.Add(new SortDescription());
                return temp;
            }
        }

        //Remove a directory from auto import
        private RelayCommand<string>? _removeBanishedPathCommand;
        public RelayCommand<string> RemoveBanishedPathCommand => _removeBanishedPathCommand ??= new(path =>
            MainViewModel.CollectionVM.CurrentCollection.Info.BanishedPaths.Remove(path!));

        #endregion

        #region Link Folders to Tag

        public CollectionViewSource FolderTagPairs
        {
            get
            {
                CollectionViewSource temp = new()
                {
                    Source = MainViewModel.CollectionVM.CurrentCollection.Info.FolderTagPairs,
                    IsLiveSortingRequested = true,
                };
                temp.SortDescriptions.Add(new SortDescription("Folder", ListSortDirection.Ascending));
                return temp;
            }
        }
        public List<Tag> AllTags => MainViewModel.CollectionVM.CurrentCollection.AllTags;

        //Remove a directory from auto import
        private RelayCommand<FolderTagPair>? _removeFolderTagPairCommand;
        public RelayCommand<FolderTagPair> RemoveFolderTagPairCommand => _removeFolderTagPairCommand ??= new(pair =>
            MainViewModel.CollectionVM.CurrentCollection.Info.FolderTagPairs.Remove(pair!));

        //Add a directory from auto import
        private RelayCommand<FolderTagPair>? _addFolderTagPairCommand;
        public RelayCommand<FolderTagPair> AddFolderTagPairCommand => _addFolderTagPairCommand ??= new(AddFolderTagPair);
        private void AddFolderTagPair(FolderTagPair? pair)
        {
            if (pair is null || String.IsNullOrWhiteSpace(pair.Folder) || pair.Tag is null || pair.Tag.IsGroup) return;
            MainViewModel.CollectionVM.CurrentCollection.Info.FolderTagPairs.AddIfMissing(pair);
        }

        private RelayCommand? _detectFolderTagPairsCommand;
        public RelayCommand DetectFolderTagPairsCommand => _detectFolderTagPairsCommand ??= new(DetectFolderTagPairs);
        private void DetectFolderTagPairs()
        {
            var splitFolders = MainViewModel.CollectionVM.CurrentCollection.AllCodices
                .Where(codex => codex.Sources.HasOfflineSource())
                .Select(codex => codex.Sources.Path)
                .SelectMany(path => path.Split("\\"))
                .ToHashSet()
                .Select(folder => @"\" + folder + @"\");

            foreach (string folder in splitFolders)
            {
                var codicesInFolder = MainViewModel.CollectionVM.CurrentCollection.AllCodices
                    .Where(codex => codex.Sources.HasOfflineSource())
                    .Where(codex => codex.Sources.Path.Contains(folder))
                    .ToList();

                if (codicesInFolder.Count < 3) continue;  //Require at least 3 codices in same folder before we can speak of a pattern

                var tagsToLink = codicesInFolder
                    .Select(codex => codex.Tags)
                    .Aggregate<IEnumerable<Tag>>((prev, next) => prev.Intersect(next))
                    .ToList();

                // if there are tags that aren't associated with any folder so far,
                // do only those to try and avoid doubles
                if (tagsToLink.Count > 1)
                {
                    var strippedTagsToLink = tagsToLink
                        .Except(MainViewModel.CollectionVM.CurrentCollection.Info.FolderTagPairs.Select(pair => pair.Tag!))
                        .ToList();
                    if (strippedTagsToLink.Any()) tagsToLink = strippedTagsToLink;
                }

                foreach (var tag in tagsToLink)
                {
                    AddFolderTagPair(new FolderTagPair(folder, tag));
                }
            }
        }

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
        public List<CodexProperty> MetaDataPreferences => Preferences.CodexProperties;
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
        private void ShowBrokenCodices()
        {
            MainViewModel.CollectionVM.FilterVM.AddFilter(new HasBrokenPathFilter());
            Application.Current.MainWindow!.Activate();
        }

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

        private void RenameFolderReferences(string? oldpath, string? newpath)
        {
            if (String.IsNullOrWhiteSpace(oldpath) || newpath is null) return;

            AmountRenamed = 0;
            foreach (Codex codex in MainViewModel.CollectionVM.CurrentCollection.AllCodices)
            {
                if (codex.Sources.HasOfflineSource() && codex.Sources.Path.Contains(oldpath))
                {
                    string updatedPath = codex.Sources.Path.Replace(oldpath, newpath);
                    //only replace path if old one was broken and new one exists
                    if (!File.Exists(codex.Sources.Path) && File.Exists(updatedPath))
                    {
                        codex.Sources.Path = updatedPath;
                        AmountRenamed++;
                    }
                }
            }
            MainViewModel.CollectionVM.CurrentCollection.SaveCodices();
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
            MainViewModel.CollectionVM.CurrentCollection.SaveCodices();
        }

        //Remove Codices with broken refs
        private RelayCommand? _deleteCodicesWithBrokenRefsCommand;
        public RelayCommand DeleteCodicesWithBrokenRefsCommand => _deleteCodicesWithBrokenRefsCommand ??= new(RemoveCodicesWithBrokenRefs);
        public void RemoveCodicesWithBrokenRefs()
        {
            MainViewModel.CollectionVM.CurrentCollection.DeleteCodices(BrokenCodices.ToList());
            BrokenCodicesChanged();
            MainViewModel.CollectionVM.CurrentCollection.SaveCodices();
            MainViewModel.CollectionVM.FilterVM.ReFilter();
        }
        #endregion

        #region Manage Data

        #region Data Path stuff

        private static string _defaultDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "COMPASS");
        public static string CompassDataPath
        {
            get
            {
                if (Path.Exists(Properties.Settings.Default.CompassDataPath))
                    return Properties.Settings.Default.CompassDataPath;
                else return _defaultDataPath;
            }

            set
            {
                try
                {
                    Properties.Settings.Default.CompassDataPath = value;
                    EnvironmentVarsService.CompassDataPath = value;
                    Properties.Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to update Compass Data Path", ex);
                }
            }
        }

        public string BindableDataPath => CompassDataPath; // used because binding to static stuff has its problems

        public string NewDataPath { get; set; } = "";

        private RelayCommand? _changeDataPathCommand;
        public RelayCommand ChangeDataPathCommand => _changeDataPathCommand ??= new(ChooseNewDataPath);
        private void ChooseNewDataPath()
        {
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog folderBrowserDialog = new()
            {
                Description = "Choose a new data location",
                //InitialDirectory = CompassDataPath
            };
            if (folderBrowserDialog.ShowDialog() == true)
            {
                string newPath = folderBrowserDialog.SelectedPath;
                SetNewDataPath(newPath);
            }
        }

        private RelayCommand? _resetDataPathCommand;
        public RelayCommand ResetDataPathCommand => _resetDataPathCommand ??= new(() => SetNewDataPath(_defaultDataPath));

        public bool SetNewDataPath(string newPath)
        {
            if (String.IsNullOrWhiteSpace(newPath) || !Path.Exists(newPath)) { return false; }

            //make sure new folder ends on /COMPASS
            string foldername = new DirectoryInfo(newPath).Name;
            if (foldername != "COMPASS")
            {
                newPath = Path.Combine(newPath, "COMPASS");
                try
                {
                    Directory.CreateDirectory(newPath);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to create the COMPASS folder at new data path location {newPath}", ex);
                    return false;
                }
            }

            if (newPath == CompassDataPath) { return false; }

            NewDataPath = newPath;

            //If there is existing data, Give users choice between moving or copying
            if (Path.Exists(CompassDataPath))
            {
                ChangeDataLocationWindow window = new(this);
                window.ShowDialog();
            }
            //If not, just change over
            else
            {
                ChangeToNewDataPath();
            }

            return true;
        }

        private AsyncRelayCommand? _moveToNewDataPathCommand;
        public AsyncRelayCommand MoveToNewDataPathCommand => _moveToNewDataPathCommand ??= new(MoveToNewDataPath);
        public async Task MoveToNewDataPath()
        {
            bool success;
            try
            {
                success = await CopyDataAsync(CompassDataPath, NewDataPath);
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warn("File copy has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
                return;
            }
            if (success)
            {
                DeleteDataLocation();
            }
        }

        private AsyncRelayCommand? _copyToNewDataPathCommand;
        public AsyncRelayCommand CopyToNewDataPathCommand => _copyToNewDataPathCommand ??= new(CopyToNewDataPath);
        private async Task CopyToNewDataPath()
        {
            try
            {
                await CopyDataAsync(CompassDataPath, NewDataPath);
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warn("File copy has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
                return;
            }

            ChangeToNewDataPath();
        }

        private RelayCommand? _changeToNewDataPathCommand;
        public RelayCommand ChangeToNewDataPathCommand => _changeToNewDataPathCommand ??= new(ChangeToNewDataPath);

        /// <summary>
        /// Sets the data path to <see cref="NewDataPath"/> and restarts the app
        /// </summary>
        public void ChangeToNewDataPath()
        {
            //save stuff in old location
            MainViewModel.CollectionVM?.CurrentCollection.Save();
            PreferencesService.GetInstance().SavePreferences();

            //change location
            CompassDataPath = NewDataPath;

            Notification changeSuccessful = new("Data path changed succesfully", $"Data path was successfully changed to {CompassDataPath}. COMPASS will now restart.");
            App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed).Show(changeSuccessful);

            //Now that datapath has been changed, don't save on close because it would save to new location
            MainViewModel.SaveOnClose = false;

            //Restart COMPASS
            var currentExecutablePath = Environment.ProcessPath;
            var args = Environment.GetCommandLineArgs();
            if (currentExecutablePath != null) Process.Start(currentExecutablePath, args);
            Application.Current.Shutdown();
        }

        private RelayCommand? _deleteDataCommand;
        public RelayCommand DeleteDataCommand => _deleteDataCommand ??= new(DeleteDataLocation);

        public void DeleteDataLocation()
        {
            try
            {
                Directory.Delete(CompassDataPath, true);
            }
            catch (Exception ex)
            {
                Logger.Error("could not delete all data", ex);
            }
            ChangeToNewDataPath();
        }

        public static async Task<bool> CopyDataAsync(string sourceDir, string destDir)
        {
            ProgressViewModel progressVM = ProgressViewModel.GetInstance();
            ProgressWindow progressWindow = new()
            {
                Owner = Application.Current.MainWindow,
            };

            var toCopy = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

            progressVM.TotalAmount = toCopy.Length;
            progressVM.ResetCounter();
            progressVM.Text = "Copying Files";

            progressWindow.Show();

            try
            {
                //Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
                }

                //Copy all the files & Replaces any files with the same name
                await Task.Run(() =>
                {
                    foreach (string sourcePath in toCopy)
                    {
                        ProgressViewModel.GlobalCancellationTokenSource.Token.ThrowIfCancellationRequested();

                        // don't copy log file, causes error because log file is open
                        if (Path.GetExtension(sourcePath) != ".log")
                        {
                            File.Copy(sourcePath, sourcePath.Replace(sourceDir, destDir), true);
                        }
                        progressVM.IncrementCounter();
                        progressVM.AddLogEntry(new LogEntry(Severity.Info, $"Copied {sourcePath}"));
                    }
                });
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warn($"Transfer was cancelled", ex);
                progressVM.ConfirmCancellation();
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not move data to {destDir}", ex);
                progressVM.Clear();
                return false;
            }
            return true;
        }
        #endregion

        private RelayCommand? _browseLocalFilesCommand;
        public RelayCommand BrowseLocalFilesCommand => _browseLocalFilesCommand ??= new(BrowseLocalFiles);
        public void BrowseLocalFiles()
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = CompassDataPath,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }

        private LoadingWindow? _lw;

        private AsyncRelayCommand? _backupLocalFilesCommand;
        public AsyncRelayCommand BackupLocalFilesCommand => _backupLocalFilesCommand ??= new(BackupLocalFiles);
        public async Task BackupLocalFiles()
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "Zip file (*.zip)|*.zip"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string targetPath = saveFileDialog.FileName;
                _lw = new("Compressing to Zip File");
                _lw.Show();

                //save first
                ApplyPreferences();
                MainViewModel.CollectionVM.CurrentCollection.Save();

                await Task.Run(() => CompressUserDataToZip(targetPath));

                _lw.Close();
            }
        }

        private void CompressUserDataToZip(string zipPath) =>
            //zip up collections, easiest with system.IO.Compression
            System.IO.Compression.ZipFile.CreateFromDirectory(CodexCollection.CollectionsPath, zipPath, System.IO.Compression.CompressionLevel.Optimal, true);

        private AsyncRelayCommand? _restoreBackupCommand;
        public AsyncRelayCommand RestoreBackupCommand => _restoreBackupCommand ??= new(RestoreBackup);
        public async Task RestoreBackup()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Zip file (*.zip)|*.zip"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string targetPath = openFileDialog.FileName;
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
            archive.ExtractToDirectory(CompassDataPath);
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

        public readonly string WebViewDataDir = Path.Combine(CompassDataPath, "WebViewData");

        #endregion

        #region Tab: About
        public string Version => "Version: " + Assembly.GetExecutingAssembly().GetName().Version?.ToString()[0..5];

        private RelayCommand? _checkForUpdatesCommand;
        public RelayCommand CheckForUpdatesCommand => _checkForUpdatesCommand ??= new(CheckForUpdates);
        private void CheckForUpdates()
        {
            AutoUpdater.Mandatory = true;
            AutoUpdater.Start();
        }
        #endregion
    }
}
