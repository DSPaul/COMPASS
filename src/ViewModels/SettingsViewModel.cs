using AutoUpdaterDotNET;
using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using Ionic.Zip;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
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

            if (File.Exists(Constants.PreferencesFilePath))
            {
                LoadPreferences();
            }
            else
            {
                Logger.log.Warn($"{Constants.PreferencesFilePath} does not exist.");
                CreateDefaultPreferences();
            }
        }

        #region singleton pattern
        private static SettingsViewModel _settingsVM;
        public static SettingsViewModel GetInstance() => _settingsVM ??= new SettingsViewModel();
        #endregion

        #region Load and Save Settings
        public static XmlWriterSettings XmlWriteSettings { get; private set; } = new() { Indent = true };
        private static SerializablePreferences AllPreferences = new();
        public void SavePreferences()
        {
            //Save OpenCodexPriority
            AllPreferences.OpenFilePriorityIDs = OpenCodexPriority.Select(pf => pf.ID).ToList();

            using var writer = XmlWriter.Create(Constants.PreferencesFilePath, XmlWriteSettings);
            System.Xml.Serialization.XmlSerializer serializer = new(typeof(SerializablePreferences));
            serializer.Serialize(writer, AllPreferences);
        }

        public void LoadPreferences()
        {
            using (var Reader = new StreamReader(Constants.PreferencesFilePath))
            {
                System.Xml.Serialization.XmlSerializer serializer = new(typeof(SerializablePreferences));
                AllPreferences = serializer.Deserialize(Reader) as SerializablePreferences;
                Reader.Close();
            }
            //put openFilePriority in right order
            OpenCodexPriority = new ObservableCollection<PreferableFunction<Codex>>(OpenCodexFunctions.OrderBy(pf => AllPreferences.OpenFilePriorityIDs.IndexOf(pf.ID)));
        }

        public void CreateDefaultPreferences() => OpenCodexPriority = new(OpenCodexFunctions);
        #endregion

        #region General Tab

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

        #region Data Tab

        #region Fix Renamed Folder
        private int _amountRenamed = 0;
        public int AmountRenamed
        {
            get => _amountRenamed;
            set
            {
                SetProperty(ref _amountRenamed, value);
                RaisePropertyChanged(nameof(RenameCompleteMessage));
            }
        }

        public string RenameCompleteMessage => $"Renamed Path Reference in {AmountRenamed} Codices";

        private RelayCommand<object[]> _renameFolderRefCommand;
        public RelayCommand<object[]> RenameFolderRefCommand => _renameFolderRefCommand ??= new(RenameFolderReferences);

        private void RenameFolderReferences(object[] args) => RenameFolderReferences((string)args[0], (string)args[1]);
        private void RenameFolderReferences(string oldpath, string newpath)
        {
            AmountRenamed = 0;
            foreach (Codex c in MainViewModel.CollectionVM.CurrentCollection.AllCodices)
            {
                if (!string.IsNullOrEmpty(c.Path) && c.Path.Contains(oldpath))
                {
                    AmountRenamed++;
                    c.Path = c.Path.Replace(oldpath, newpath);
                }

            }
        }
        #endregion

        private ActionCommand _browseLocalFilesCommand;
        public ActionCommand BrowseLocalFilesCommand => _browseLocalFilesCommand ??= new(BrowseLocalFiles);
        public void BrowseLocalFiles()
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = Constants.CompassDataPath,
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

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string targetPath = saveFileDialog.FileName;
                lw = new("Compressing to Zip File");
                lw.Show();

                //save first
                MainViewModel.CollectionVM.CurrentCollection.SaveCodices();
                MainViewModel.CollectionVM.CurrentCollection.SaveTags();
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

            if (openFileDialog.ShowDialog() == DialogResult.OK)
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
            zip.AddFile(Constants.PreferencesFilePath, "");
            zip.Save(targetPath);
        }

        private void ExtractZip(object sender, DoWorkEventArgs e)
        {
            string sourcePath = e.Argument as string;
            using ZipFile zip = ZipFile.Read(sourcePath);
            zip.ExtractAll(Constants.CompassDataPath, ExtractExistingFileAction.OverwriteSilently);
        }

        private void CreateZipDone(object sender, RunWorkerCompletedEventArgs e) => lw.Close();

        private void ExtractZipDone(object sender, RunWorkerCompletedEventArgs e)
        {
            //restore collection that was open
            MainViewModel.CollectionVM.ChangeCollection(Properties.Settings.Default.StartupCollection);
            lw.Close();
        }

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

        #region What's New Tab
        private string _releaseNotes;
        public string ReleaseNotes
        {
            get => _releaseNotes;
            set => SetProperty(ref _releaseNotes, value);
        }
        #endregion

        #region About Tab
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
