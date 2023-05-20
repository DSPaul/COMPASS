using AutoUpdaterDotNET;
using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using Ionic.Zip;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Xml;

namespace COMPASS.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private SettingsViewModel()
        {
            LoadGlobalPreferences();
            _allPreferences.Init();
        }

        #region singleton pattern
        private static SettingsViewModel _settingsVM;
        public static SettingsViewModel GetInstance() => _settingsVM ??= new SettingsViewModel();
        #endregion

        public MainViewModel MVM { get; set; }

        #region Load and Save Settings
        public static string PreferencesFilePath => Path.Combine(CompassDataPath, "Preferences.xml");
        public static XmlWriterSettings XmlWriteSettings { get; private set; } = new() { Indent = true };
        private static GlobalPreferences _allPreferences = new();
        public void SavePreferences()
        {
            //Prep OpenCodexPriority for save
            _allPreferences.OpenFilePriorityIDs = OpenCodexPriority.Select(pf => pf.ID).ToList();

            using var writer = XmlWriter.Create(PreferencesFilePath, XmlWriteSettings);
            System.Xml.Serialization.XmlSerializer serializer = new(typeof(GlobalPreferences));
            serializer.Serialize(writer, _allPreferences);

            //Convert list back to dict because dict does not support two way binding
            MainViewModel.CollectionVM.CurrentCollection.Info.FiletypePreferences = FiletypePreferences.ToDictionary(x => x.Key, x => x.Value);
        }

        public void LoadGlobalPreferences()
        {
            if (File.Exists(PreferencesFilePath))
            {
                using (var reader = new StreamReader(PreferencesFilePath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new(typeof(GlobalPreferences));
                    _allPreferences = serializer.Deserialize(reader) as GlobalPreferences;
                    reader.Close();
                }
                //put openFilePriority in right order
                OpenCodexPriority = new(_openCodexFunctions.OrderBy(pf => _allPreferences.OpenFilePriorityIDs.IndexOf(pf.ID)));
            }
            else
            {
                Logger.Warn($"{PreferencesFilePath} does not exist.", new FileNotFoundException());
                CreateDefaultPreferences();
            }
        }

        public void Refresh()
        {
            //Tell the window that the FiletypePreferences dict might have changed so it needs to fetch it again
            _filetypePreferences = null;
            RaisePropertyChanged(nameof(FiletypePreferences));
        }

        public void CreateDefaultPreferences() => OpenCodexPriority = new(_openCodexFunctions);
        #endregion

        #region Tab: Preferences

        #region File Source Preference
        //list with possible functions to open a file
        private readonly List<PreferableFunction<Codex>> _openCodexFunctions = new()
            {
                new PreferableFunction<Codex>("Web Version", CodexViewModel.OpenCodexOnline,0),
                new PreferableFunction<Codex>("Local File", CodexViewModel.OpenCodexLocally,1)
            };
        //same ordered version of the list
        private ObservableCollection<PreferableFunction<Codex>> _openCodexPriority;
        public ObservableCollection<PreferableFunction<Codex>> OpenCodexPriority
        {
            get => _openCodexPriority;
            set => SetProperty(ref _openCodexPriority, value);
        }
        #endregion

        #region Virtualization Preferences

        public bool DoVirtualizationList
        {
            get => Properties.Settings.Default.DoVirtualizationList;
            set
            {
                Properties.Settings.Default.DoVirtualizationList = value;
                MVM.CurrentLayout.RaisePreferencesChanged();
            }
        }

        public bool DoVirtualizationCard
        {
            get => Properties.Settings.Default.DoVirtualizationCard;
            set
            {
                Properties.Settings.Default.DoVirtualizationCard = value;
                MVM.CurrentLayout.RaisePreferencesChanged();
            }
        }

        public bool DoVirtualizationTile
        {
            get => Properties.Settings.Default.DoVirtualizationTile;
            set
            {
                Properties.Settings.Default.DoVirtualizationTile = value;
                MVM.CurrentLayout.RaisePreferencesChanged();
            }
        }

        public int VirtualizationThresholdList
        {
            get => Properties.Settings.Default.VirtualizationThresholdList;
            set
            {
                Properties.Settings.Default.VirtualizationThresholdList = value;
                MVM.CurrentLayout.RaisePreferencesChanged();
            }
        }

        public int VirtualizationThresholdCard
        {
            get => Properties.Settings.Default.VirtualizationThresholdCard;
            set
            {
                Properties.Settings.Default.VirtualizationThresholdCard = value;
                MVM.CurrentLayout.RaisePreferencesChanged();
            }
        }

        public int VirtualizationThresholdTile
        {
            get => Properties.Settings.Default.VirtualizationThresholdTile;
            set
            {
                Properties.Settings.Default.VirtualizationThresholdTile = value;
                MVM.CurrentLayout.RaisePreferencesChanged();
            }
        }
        #endregion

        #endregion

        #region Tab: Import

        //Open folder in explorer
        private RelayCommand<string> _showInExplorerCommand;
        public RelayCommand<string> ShowInExplorerCommand => _showInExplorerCommand ??= new(Utils.ShowInExplorer);

        #region Auto import folders
        public CollectionViewSource AutoImportDirectories
        {
            get
            {
                CollectionViewSource temp = new()
                {
                    Source = MainViewModel.CollectionVM.CurrentCollection.Info.AutoImportDirectories,
                    IsLiveSortingRequested = true,
                };
                temp.SortDescriptions.Add(new SortDescription());
                return temp;
            }
        }

        //Remove a directory from auto import
        private RelayCommand<string> _removeAutoImportDirectoryCommand;
        public RelayCommand<string> RemoveAutoImportDirectoryCommand => _removeAutoImportDirectoryCommand ??= new(dir =>
            MainViewModel.CollectionVM.CurrentCollection.Info.AutoImportDirectories.Remove(dir));

        //Add a directory from auto import
        private RelayCommand<string> _addAutoImportDirectoryCommand;
        public RelayCommand<string> AddAutoImportDirectoryCommand => _addAutoImportDirectoryCommand ??= new(AddAutoImportDirectory);

        private void AddAutoImportDirectory(string dir)
        {
            if (!String.IsNullOrWhiteSpace(dir))
            {
                MainViewModel.CollectionVM.CurrentCollection.Info.AutoImportDirectories.AddIfMissing(dir);
            }
        }

        //Add a directory from auto import
        private ActionCommand _pickAutoImportDirectoryCommand;
        public ActionCommand PickAutoImportDirectoryCommand => _pickAutoImportDirectoryCommand ??= new(() => AddAutoImportDirectory(Utils.PickFolder()));

        //File types to import
        private List<ObservableKeyValuePair<string, bool>> _filetypePreferences;
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
        private RelayCommand<string> _removeBanishedPathCommand;
        public RelayCommand<string> RemoveBanishedPathCommand => _removeBanishedPathCommand ??= new(path =>
            MainViewModel.CollectionVM.CurrentCollection.Info.BanishedPaths.Remove(path));

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
        private RelayCommand<FolderTagPair> _removeFolderTagPairCommand;
        public RelayCommand<FolderTagPair> RemoveFolderTagPairCommand => _removeFolderTagPairCommand ??= new(pair =>
            MainViewModel.CollectionVM.CurrentCollection.Info.FolderTagPairs.Remove(pair));

        //Add a directory from auto import
        private RelayCommand<FolderTagPair> _addFolderTagPairCommand;
        public RelayCommand<FolderTagPair> AddFolderTagPairCommand => _addFolderTagPairCommand ??= new(AddFolderTagPair);
        private void AddFolderTagPair(FolderTagPair pair)
        {
            if (String.IsNullOrWhiteSpace(pair.Folder) || pair.Tag is null || pair.Tag.IsGroup) return;
            MainViewModel.CollectionVM.CurrentCollection.Info.FolderTagPairs.AddIfMissing(pair);
        }

        private ActionCommand _detectFolderTagPairsCommand;
        public ActionCommand DetectFolderTagPairsCommand => _detectFolderTagPairsCommand ??= new(DetectFolderTagPairs);
        private void DetectFolderTagPairs()
        {
            var splitFolders = MainViewModel.CollectionVM.CurrentCollection.AllCodices
                .Where(codex => codex.HasOfflineSource())
                .Select(codex => codex.Path)
                .SelectMany(path => path.Split("\\"))
                .ToHashSet()
                .Select(folder => @"\" + folder + @"\");
            foreach (string folder in splitFolders)
            {
                var codicesInFolder = MainViewModel.CollectionVM.CurrentCollection.AllCodices
                    .Where(codex => codex.HasOfflineSource())
                    .Where(codex => codex.Path.Contains(folder))
                    .ToList();

                if (codicesInFolder.Count() < 3) continue;  //Require at least 3 codices in same folder before we can speak of a pattern

                var tagsToLink = codicesInFolder
                    .Select(codex => codex.Tags)
                    .Aggregate<IEnumerable<Tag>>((prev, next) => prev.Intersect(next))
                    .ToList();

                // if there are tags that aren't associated with any folder so far,
                // do only those to try and avoid doubles
                if (tagsToLink.Count() > 1)
                {
                    var strippedTagsToLink = tagsToLink
                        .Except(MainViewModel.CollectionVM.CurrentCollection.Info.FolderTagPairs.Select(pair => pair.Tag))
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
            get => Properties.Settings.Default.AutoLinkFolderTagSameName;
            set
            {
                Properties.Settings.Default.AutoLinkFolderTagSameName = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #endregion

        #region Tab: Metadata
        public List<CodexProperty> MetaDataPreferences => _allPreferences.CodexProperties;
        #endregion

        #region Tab: Data

        #region Fix Broken refs

        public IEnumerable<Codex> BrokenCodices => MainViewModel.CollectionVM.CurrentCollection.AllCodices
               .Where(codex => codex.HasOfflineSource()) //do this check so message doesn't count codices that never had a path to begin with
               .Where(codex => !Path.Exists(codex.Path));
        public int BrokenCodicesAmount => BrokenCodices.Count();
        public string BrokenCodicesMessage => $"Broken references detected: {BrokenCodicesAmount}.";

        private void BrokenCodicesChanged()
        {
            RaisePropertyChanged(nameof(BrokenCodices));
            RaisePropertyChanged(nameof(BrokenCodicesAmount));
            RaisePropertyChanged(nameof(BrokenCodicesMessage));
        }

        private ActionCommand _showBrokenCodicesCommand;
        public ActionCommand ShowBrokenCodicesCommand => _showBrokenCodicesCommand ??= new(ShowBrokenCodices);
        private void ShowBrokenCodices()
        {
            MainViewModel.CollectionVM.FilterVM.AddFilter(new(Filter.FilterType.HasBrokenPath));
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
                RaisePropertyChanged(nameof(RenameCompleteMessage));
                BrokenCodicesChanged();
            }
        }

        public string RenameCompleteMessage => $"Renamed Path Reference in {AmountRenamed} Files.";

        private RelayCommand<object[]> _renameFolderRefCommand;
        public RelayCommand<object[]> RenameFolderRefCommand => _renameFolderRefCommand ??= new(RenameFolderReferences);

        private void RenameFolderReferences(object[] args) => RenameFolderReferences((string)args[0], (string)args[1]);
        private void RenameFolderReferences(string oldpath, string newpath)
        {
            AmountRenamed = 0;
            foreach (Codex codex in MainViewModel.CollectionVM.CurrentCollection.AllCodices)
            {
                if (codex.HasOfflineSource() && codex.Path.Contains(oldpath) && !String.IsNullOrWhiteSpace(oldpath) && newpath is not null)
                {
                    string updatedPath = codex.Path.Replace(oldpath, newpath);
                    //only replace path if old one was broken and new one exists
                    if (!File.Exists(codex.Path) && File.Exists(updatedPath))
                    {
                        codex.Path = updatedPath;
                        AmountRenamed++;
                    }
                }
            }
            MainViewModel.CollectionVM.CurrentCollection.SaveCodices();
        }

        //remove refs from codices
        private ActionCommand _removeBrokenRefsCommand;
        public ActionCommand RemoveBrokenRefsCommand => _removeBrokenRefsCommand ??= new(RemoveBrokenReferences);
        public void RemoveBrokenReferences()
        {
            foreach (Codex codex in BrokenCodices)
            {
                codex.Path = "";
            }
            BrokenCodicesChanged();
            MainViewModel.CollectionVM.CurrentCollection.SaveCodices();
        }

        //Remove Codices with broken refs
        private ActionCommand _deleteCodicesWithBrokenRefsCommand;
        public ActionCommand DeleteCodicesWithBrokenRefsCommand => _deleteCodicesWithBrokenRefsCommand ??= new(RemoveCodicesWithBrokenRefs);
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
                Properties.Settings.Default.CompassDataPath = value;
                Properties.Settings.Default.Save();
            }
        }

        public string BindableDataPath => CompassDataPath; // used because binding to static stuff has its problems

        public string NewDataPath { get; set; }

        private ActionCommand _changeDataPathCommand;
        public ActionCommand ChangeDataPathCommand => _changeDataPathCommand ??= new(ChooseNewDataPath);
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

        private ActionCommand _resetDataPathCommand;
        public ActionCommand ResetDataPathCommand => _resetDataPathCommand ??= new(() => SetNewDataPath(_defaultDataPath));

        private void SetNewDataPath(string newPath)
        {
            if (String.IsNullOrWhiteSpace(newPath) || !Path.Exists(newPath)) { return; }

            //make sure new folder ends on /COMPASS
            string foldername = new DirectoryInfo(newPath).Name;
            if (foldername != "COMPASS")
            {
                newPath = Path.Combine(newPath, "COMPASS");
                Directory.CreateDirectory(newPath);
            }

            if (newPath == CompassDataPath) { return; }

            NewDataPath = newPath;

            //Give users choice between moving or copying
            ChangeDataLocationWindow window = new(this);
            window.ShowDialog();
        }

        private ActionCommand _moveToNewDataPathCommand;
        public ActionCommand MoveToNewDataPathCommand => _moveToNewDataPathCommand ??= new(MoveToNewDataPath);
        public async void MoveToNewDataPath()
        {
            bool success;
            try
            {
                success = await CopyData(CompassDataPath, NewDataPath);
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

        private ActionCommand _copyToNewDataPathCommand;
        public ActionCommand CopyToNewDataPathCommand => _copyToNewDataPathCommand ??=
            new(async () =>
            {
                try
                {
                    await CopyData(CompassDataPath, NewDataPath);
                }
                catch (OperationCanceledException ex)
                {
                    Logger.Warn("File copy has been cancelled", ex);
                    await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
                    return;
                }

                ChangeToNewDataPath();
            });

        private ActionCommand _changeToNewDataPathCommand;
        public ActionCommand ChangeToNewDataPathCommand => _changeToNewDataPathCommand ??= new(ChangeToNewDataPath);
        public void ChangeToNewDataPath()
        {
            MainViewModel.CollectionVM.CurrentCollection.Save();

            CompassDataPath = NewDataPath;

            //Restart COMPASS
            var currentExecutablePath = Environment.ProcessPath;
            var args = Environment.GetCommandLineArgs();
            if (currentExecutablePath != null) Process.Start(currentExecutablePath, args);
            Application.Current.Shutdown();
        }

        private ActionCommand _deleteDataCommand;
        public ActionCommand DeleteDataCommand => _deleteDataCommand ??= new(DeleteDataLocation);

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

        public static async Task<bool> CopyData(string sourceDir, string destDir)
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
                        Application.Current.Dispatcher.Invoke(() =>
                            progressVM.Log.Add(new LogEntry(LogEntry.MsgType.Info, $"Copied {sourcePath}")));
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not move data to {destDir}", ex);
                return false;
            }
            return true;
        }
        #endregion

        private ActionCommand _browseLocalFilesCommand;
        public ActionCommand BrowseLocalFilesCommand => _browseLocalFilesCommand ??= new(BrowseLocalFiles);
        public void BrowseLocalFiles()
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = CompassDataPath,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }

        private readonly BackgroundWorker _createZipWorker = new();
        private readonly BackgroundWorker _extractZipWorker = new();
        private LoadingWindow _lw;

        private ActionCommand _backupLocalFilesCommand;
        public ActionCommand BackupLocalFilesCommand => _backupLocalFilesCommand ??= new(BackupLocalFiles);
        public void BackupLocalFiles()
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
                MainViewModel.CollectionVM.CurrentCollection.Save();
                SavePreferences();

                _createZipWorker.DoWork += CreateZip;
                _createZipWorker.RunWorkerCompleted += CreateZipDone;

                _createZipWorker.RunWorkerAsync(targetPath);
            }
        }

        private ActionCommand _restoreBackupCommand;
        public ActionCommand RestoreBackupCommand => _restoreBackupCommand ??= new(RestoreBackup);
        public void RestoreBackup()
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

                _extractZipWorker.DoWork += ExtractZip;
                _extractZipWorker.RunWorkerCompleted += ExtractZipDone;

                _extractZipWorker.RunWorkerAsync(targetPath);
            }
        }

        private void CreateZip(object sender, DoWorkEventArgs e)
        {
            string targetPath = e.Argument as string;
            using ZipFile zip = new();
            zip.AddDirectory(CodexCollection.CollectionsPath, "Collections");
            zip.AddFile(PreferencesFilePath, "");
            zip.Save(targetPath);
        }

        private void ExtractZip(object sender, DoWorkEventArgs e)
        {
            string sourcePath = e.Argument as string;
            using ZipFile zip = ZipFile.Read(sourcePath);
            zip.ExtractAll(CompassDataPath, ExtractExistingFileAction.OverwriteSilently);
        }

        private void CreateZipDone(object sender, RunWorkerCompletedEventArgs e) => _lw.Close();

        private void ExtractZipDone(object sender, RunWorkerCompletedEventArgs e)
        {
            //restore collection that was open
            MainViewModel.CollectionVM.CurrentCollection = new(Properties.Settings.Default.StartupCollection);
            _lw.Close();
        }
        #endregion

        #endregion

        //for debugging only
        public void RegenAllThumbnails()
        {
            foreach (Codex codex in MainViewModel.CollectionVM.CurrentCollection.AllCodices)
            {
                //codex.Thumbnail = codex.CoverArt.Replace("CoverArt", "Thumbnails");
                CoverFetcher.CreateThumbnail(codex);
            }
        }

        #region Tab: What's New

        public readonly string WebViewDataDir = Path.Combine(CompassDataPath, "WebViewData");

        #endregion

        #region Tab: About
        public string Version => "Version: " + Assembly.GetExecutingAssembly().GetName().Version?.ToString()[0..5];

        private ActionCommand _checkForUpdatesCommand;
        public ActionCommand CheckForUpdatesCommand => _checkForUpdatesCommand ??= new(CheckForUpdates);
        private void CheckForUpdates()
        {
            AutoUpdater.Mandatory = true;
            AutoUpdater.Start();
        }
        #endregion
    }
}
