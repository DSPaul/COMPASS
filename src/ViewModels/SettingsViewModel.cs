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
using System.Xml;

namespace COMPASS.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private SettingsViewModel()
        {
            //find name of current release-notes
            var version = Assembly.GetEntryAssembly().GetName().Version.ToString()[..5];
            if (File.Exists($"release-notes-{version}.md"))
            {
                ReleaseNotes = File.ReadAllText($"release-notes-{version}.md");
            }

            LoadPreferences();
        }

        #region singleton pattern
        private static SettingsViewModel _settingsVM;
        public static SettingsViewModel GetInstance() => _settingsVM ??= new SettingsViewModel();
        #endregion

        #region Load and Save Settings
        public static string PreferencesFilePath => Path.Combine(CompassDataPath, "Preferences.xml");
        public static XmlWriterSettings XmlWriteSettings { get; private set; } = new() { Indent = true };
        private static GlobalPreferences AllPreferences = new();
        public void SavePreferences()
        {
            //Save OpenCodexPriority
            AllPreferences.OpenFilePriorityIDs = OpenCodexPriority.Select(pf => pf.ID).ToList();

            using var writer = XmlWriter.Create(PreferencesFilePath, XmlWriteSettings);
            System.Xml.Serialization.XmlSerializer serializer = new(typeof(GlobalPreferences));
            serializer.Serialize(writer, AllPreferences);
        }

        public void LoadPreferences()
        {
            if (File.Exists(PreferencesFilePath))
            {
                using (var Reader = new StreamReader(PreferencesFilePath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new(typeof(GlobalPreferences));
                    AllPreferences = serializer.Deserialize(Reader) as GlobalPreferences;
                    Reader.Close();
                }
                //put openFilePriority in right order
                OpenCodexPriority = new(OpenCodexFunctions.OrderBy(pf => AllPreferences.OpenFilePriorityIDs.IndexOf(pf.ID)));
            }
            else
            {
                Logger.Warn($"{PreferencesFilePath} does not exist.", new FileNotFoundException());
                CreateDefaultPreferences();
            }
        }

        public void CreateDefaultPreferences() => OpenCodexPriority = new(OpenCodexFunctions);
        #endregion

        #region Tab: Preferences

        #region File Source Preference
        //list with possible functions to open a file
        private readonly List<PreferableFunction<Codex>> OpenCodexFunctions = new()
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

        #endregion

        #region Tab: Sources

        public ObservableCollection<string> AutoImportDirectories
        {
            get
            {
                MainViewModel.CollectionVM.CurrentCollection.Info.AutoImportDirectories.CollectionChanged += (e, v) => RaisePropertyChanged(nameof(AutoImportDirectories));
                return MainViewModel.CollectionVM.CurrentCollection.Info.AutoImportDirectories;
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
            if (!String.IsNullOrEmpty(dir))
            {
                MainViewModel.CollectionVM.CurrentCollection.Info.AutoImportDirectories.AddIfMissing(dir);
            }
        }

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
            System.Windows.Application.Current.MainWindow.Activate();
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
            };
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
            bool success = await CopyData(CompassDataPath, NewDataPath);
            if (success)
            {
                DeleteDataLocation();
            }
        }

        private ActionCommand _copyToNewDataPathCommand;
        public ActionCommand CopyToNewDataPathCommand => _copyToNewDataPathCommand ??=
            new(async () =>
            {
                await CopyData(CompassDataPath, NewDataPath);
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
            Process.Start(currentExecutablePath, args);
            Application.Current.Shutdown();
        }

        private ActionCommand _deleteDataCommand;
        public ActionCommand DeleteDataCommand => _deleteDataCommand ??= new(DeleteDataLocation);

        public void DeleteDataLocation()
        {
            Directory.Delete(CompassDataPath, true);
            ChangeToNewDataPath();
        }

        public static async Task<bool> CopyData(string sourceDir, string destDir)
        {
            ProgressViewModel progressVM = new();
            ProgressWindow progressWindow = new(progressVM)
            {
                Owner = Application.Current.MainWindow,
            };

            var toCopy = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
            int totalToDo = toCopy.Length;
            int counter = 0;

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
                        progressVM.Text = $"Files Copied: {counter} / {totalToDo}";

                        // don't copy log file, causes error because log file is open
                        if (Path.GetExtension(sourcePath) != ".log")
                        {
                            File.Copy(sourcePath, sourcePath.Replace(sourceDir, destDir), true);
                        }
                        counter++;
                        progressVM.SetPercentage(counter, totalToDo);
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

        private readonly BackgroundWorker createZipWorker = new();
        private readonly BackgroundWorker extractZipWorker = new();
        private LoadingWindow lw;

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
                lw = new("Compressing to Zip File");
                lw.Show();

                //save first
                MainViewModel.CollectionVM.CurrentCollection.Save();
                SavePreferences();

                createZipWorker.DoWork += CreateZip;
                createZipWorker.RunWorkerCompleted += CreateZipDone;

                createZipWorker.RunWorkerAsync(targetPath);
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
                lw = new("Restoring Backup");
                lw.Show();

                extractZipWorker.DoWork += ExtractZip;
                extractZipWorker.RunWorkerCompleted += ExtractZipDone;

                extractZipWorker.RunWorkerAsync(targetPath);
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

        private void CreateZipDone(object sender, RunWorkerCompletedEventArgs e) => lw.Close();

        private void ExtractZipDone(object sender, RunWorkerCompletedEventArgs e)
        {
            //restore collection that was open
            MainViewModel.CollectionVM.CurrentCollection = new(Properties.Settings.Default.StartupCollection);
            lw.Close();
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
        private string _releaseNotes;
        public string ReleaseNotes
        {
            get => _releaseNotes;
            set => SetProperty(ref _releaseNotes, value);
        }
        #endregion

        #region Tab: About
        public string Version => "Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString()[0..5];

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
