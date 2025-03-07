using Autofac;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.ViewModels.Import
{
    public class ImportFolderViewModel : WizardViewModel
    {
        #region CTOR
        public ImportFolderViewModel(bool manuallyTriggered) : this(MainViewModel.CollectionVM.CurrentCollection, manuallyTriggered) { }
        public ImportFolderViewModel(CodexCollection targetCollection, bool manuallyTriggered)
        {
            _targetCollection = targetCollection;
            _manuallyTriggered = manuallyTriggered;
        }
        #endregion

        private readonly CodexCollection _targetCollection;
        private readonly bool _manuallyTriggered;

        public string WindowTitle => _manuallyTriggered ? "Import Folder(s)" : "AutoImport";

        public List<string> RecursiveDirectories { get; set; } = [];
        public List<string> NonRecursiveDirectories { get; set; } = [];
        public List<string> Files { get; set; } = [];
        public List<Folder> ExistingFolders { get; set; } = [];


        private bool _autoImportFolders = true;
        public bool AddAutoImportFolders
        {
            get => _autoImportFolders;
            set => SetProperty(ref _autoImportFolders, value);
        }

        public async Task Import()
        {
            //if no files are given to import, don't
            if (_manuallyTriggered && RecursiveDirectories.Count + NonRecursiveDirectories.Count + ExistingFolders.Count + Files.Count == 0)
            {
                bool success = await LetUserSelectFolders();
                if (!success) return;
            }

            var toImport = GetPathsToImport();
            if (toImport.Any())
            {
                toImport = LetUserFilterToImport(toImport);
            }
            else if (_manuallyTriggered)
            {
                Notification noFilesFound = new("No files found", "The selected folder did not contain any files.");
                var windowedNotificationService = App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed);
                await windowedNotificationService.Show(noFilesFound);
            }
            await ImportViewModel.ImportFilesAsync(toImport, _targetCollection);
        }

        /// <summary>
        /// Lets a user select folders using a dialog 
        /// and stores them in RecursiveDirectories
        /// </summary>
        /// <returns> A bool indicating whether the user successfully chose a set of folders </returns>
        private async Task<bool> LetUserSelectFolders()
        {
            var selectedPaths = await IOService.TryPickFolders().ConfigureAwait(false);
            RecursiveDirectories = [.. selectedPaths];
            return selectedPaths.Any();
        }

        /// <summary>
        /// Get a list of all the file paths that are not banned
        /// because of banishment or due to file extension preference
        /// That are either in FileName or in a folder in RecursiveDirectories
        /// </summary>
        /// <returns></returns>
        private List<string> GetPathsToImport()
        {
            // 1. Unroll the recursive folders and add them to non-recursive folders
            Queue<string> toSearch = new(RecursiveDirectories);
            while (toSearch.Any())
            {
                string currentFolder = toSearch.Dequeue();

                if (Directory.Exists(currentFolder))
                {
                    NonRecursiveDirectories.Add(currentFolder);
                    IEnumerable<string> subfolders;
                    try
                    {
                        subfolders = Directory.GetDirectories(currentFolder);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to get subfolders of {currentFolder}", ex);
                        subfolders = [];
                    }
                    foreach (string dir in subfolders)
                    {
                        toSearch.Enqueue(dir);
                    }
                }
            }

            //2. Build a list with all the files to import
            List<string> toImport = [..Files];
            foreach (var folder in NonRecursiveDirectories)
            {
                toImport.AddRange(IOService.TryGetFilesInFolder(folder));
            }

            //3. Filter out doubles and banished paths
            return toImport.Distinct().Except(_targetCollection.Info.BanishedPaths).ToList();
        }

        /// <summary>
        /// Shows an <see cref="ImportFolderWizard"/> if certain conditions are met
        /// </summary>
        /// <param name="toImport"></param>
        /// <returns></returns>
        private List<string> LetUserFilterToImport(IList<string> toImport)
        {
            //Add SubFolders Step
            if (_manuallyTriggered)
            {
                Steps.Add("SubFolders");

                ImportAmount = toImport.Count;

                //Build the checkable folder Tree
                IEnumerable<Folder> folderObjects = RecursiveDirectories.Select(f => new Folder(f));
                CheckableFolders = folderObjects.Select(f => new CheckableTreeNode<Folder>(f, containerOnly: false)).ToList();

                foreach (Folder folder in ExistingFolders)
                {
                    //first make a checkable Folder with all the subfolders, then uncheck those not in the original
                    var checkableFolder = new CheckableTreeNode<Folder>(new Folder(folder.FullPath), containerOnly: false);
                    var chosenSubFolderPaths = folder.SubFolders.Flatten().Select(sf => sf.FullPath).ToList();
                    foreach (var subFolder in checkableFolder.Children.Flatten())
                    {
                        subFolder.IsChecked = chosenSubFolderPaths.Contains(subFolder.Item.FullPath);
                    }
                    CheckableFolders.Add(checkableFolder);
                }
            }

            //find how many files of each filetype
            var toImportGrouped = toImport.GroupBy(p => Path.GetExtension(p)).ToList();
            var extensions = toImportGrouped.Select(x => x.Key).ToList();
            var newExtensions = extensions.Except(_targetCollection.Info.FiletypePreferences.Keys).ToList();

            //Add Extensions Step
            if (newExtensions.Any())
            {
                Steps.Add("Extensions");

                KnownFileTypes = toImportGrouped
                    .Where(grouping => _targetCollection.Info.FiletypePreferences.ContainsKey(grouping.Key))
                    .Select(x => new FileTypeInfo(x.Key, _targetCollection.Info.FiletypePreferences[x.Key], x.Count())).ToList();

                UnknownFileTypes = toImportGrouped
                    .Where(grouping => !_targetCollection.Info.FiletypePreferences.ContainsKey(grouping.Key))
                    .Select(x => new FileTypeInfo(x.Key, true, x.Count())).ToList();
            }

            //Show the wizard
            if (Steps.Any())
            {
                ImportFolderWizard importFolderWindow = new(this);

                //TODO, ShowDialog is now async, find out how we wait until completion
                var dialogResult = importFolderWindow.ShowDialog(App.MainWindow);
                if (!dialogResult.IsCompletedSuccessfully) return [];
            }

            //filer toImport so it only contains files from checked subfolders
            if (CheckableFolders.Any())
            {
                List<string> toImportBySubFolders = [];
                var checkedFolders = CheckableTreeNode<Folder>.GetCheckedItems(CheckableFolders).Flatten();
                foreach (var folder in checkedFolders)
                {
                    toImportBySubFolders.AddRange(IOService.TryGetFilesInFolder(folder.FullPath));
                }
                toImport = toImport.Intersect(toImportBySubFolders).ToList();
            }

            //filter out File types and return
            return toImport.Where(path => _targetCollection.Info.FiletypePreferences[Path.GetExtension(path)]).ToList();
        }

        #region Subfolder Select Step
        public int ImportAmount { get; set; }

        public List<CheckableTreeNode<Folder>> CheckableFolders { get; set; } = [];
        #endregion

        #region File Type Selection Step
        private IEnumerable<FileTypeInfo> _knownFileTypes = [];
        public IEnumerable<FileTypeInfo> KnownFileTypes
        {
            get => _knownFileTypes;
            set => SetProperty(ref _knownFileTypes, value);
        }

        private IEnumerable<FileTypeInfo> _unknownFileTypes = [];
        public IEnumerable<FileTypeInfo> UnknownFileTypes
        {
            get => _unknownFileTypes;
            set => SetProperty(ref _unknownFileTypes, value);
        }

        //helper class for file type selection during folder import
        public class FileTypeInfo
        {
            public FileTypeInfo(string extension, bool shouldImport, int fileCount = 0)
            {
                FileExtension = extension;
                _fileCount = fileCount;
                ShouldImport = shouldImport;
            }

            private readonly int _fileCount;
            public string FileExtension { get; }
            public bool ShouldImport { get; set; }
            public string DisplayText => $"{FileExtension} ({_fileCount} file{(_fileCount > 1 ? @"s" : @"")})";
        }
        #endregion

        public override Task Finish()
        {
            //Update the Auto Import Folders
            if (AddAutoImportFolders)
            {
                //go over every folder and set the HasAllSubFolder Flag
                foreach (var checkableFolder in CheckableFolders.Flatten())
                {
                    checkableFolder.Item.HasAllSubFolders =
                        checkableFolder.IsChecked == true &&
                        checkableFolder.Children.All(child => child.IsChecked == true); //need this check as well because folder might also be checked without any children
                }

                //Remove the existingFolders as they will be replaced
                foreach (Folder folder in ExistingFolders)
                {
                    _targetCollection.Info.AutoImportFolders.Remove(folder);
                }

                //Add the folder to the AutoImportFolders
                var checkedFolders = CheckableTreeNode<Folder>.GetCheckedItems(CheckableFolders);
                foreach (Folder folder in checkedFolders)
                {
                    if (_targetCollection.Info.AutoImportFolders.All(f => f.FullPath != folder.FullPath))
                    {
                        _targetCollection.Info.AutoImportFolders.Add(folder);
                    }
                }
            }

            //update the collections file type preferences
            foreach (var filetypeHelper in UnknownFileTypes)
            {
                _targetCollection.Info.FiletypePreferences.TryAdd(filetypeHelper.FileExtension, filetypeHelper.ShouldImport);
            }
            foreach (var filetypeHelper in KnownFileTypes)
            {
                _targetCollection.Info.FiletypePreferences[filetypeHelper.FileExtension] = filetypeHelper.ShouldImport;
            }

            CloseAction?.Invoke();
            return Task.CompletedTask;
        }
    }
}
