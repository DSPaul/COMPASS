using COMPASS.Models;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.ViewModels.Import
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
        private bool _manuallyTriggered = true;

        public string WindowTitle => _manuallyTriggered ? "Import Folder(s)" : "AutoImport";

        public List<string> RecursiveDirectories { get; set; } = new();
        public List<string> NonRecursiveDirectories { get; set; } = new();
        public List<string> Files { get; set; } = new();
        public List<Folder> ExistingFolders { get; set; } = new();


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
                LetUserSelectFolders();
            }

            var toImport = GetPathsToImport();
            toImport = LetUserFilterToImport(toImport);
            await ImportViewModel.ImportFilesAsync(toImport, _targetCollection);
        }

        /// <summary>
        /// Lets a user select folders using a dialog 
        /// and stores them in RecursiveDirectories
        /// </summary>
        /// <returns>A list of paths </returns>
        private void LetUserSelectFolders()
        {
            string[] selectedPath = IOService.PickFolders();

            if (!selectedPath.Any()) return;

            RecursiveDirectories = selectedPath.ToList();
        }

        /// <summary>
        /// Get a list of all the file paths that are not banned
        /// because of banishent or due to file extension preference
        /// That are either in FileName or in a folder in RecursiveDirectories
        /// </summary>
        /// <returns></returns>
        private List<string> GetPathsToImport()
        {
            // 1. Unroll the recursive folders and add them to non recursive folders
            Queue<string> toSearch = new(RecursiveDirectories);
            while (toSearch.Any())
            {
                string currentFolder = toSearch.Dequeue();

                if (Directory.Exists(currentFolder))
                {
                    NonRecursiveDirectories.Add(currentFolder);
                    foreach (string dir in Directory.GetDirectories(currentFolder))
                    {
                        toSearch.Enqueue(dir);
                    }
                }
            }

            //2. Build a list with all the files to import
            List<string> toImport = new(Files);
            foreach (var folder in NonRecursiveDirectories)
            {
                if (Directory.Exists(folder))
                {
                    toImport.AddRange(Directory.GetFiles(folder));
                }
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
                CheckableFolders = folderObjects.Select(f => new CheckableTreeNode<Folder>(f)).ToList();

                foreach (Folder folder in ExistingFolders)
                {
                    //first make a checkable Folder with all the subfolders, then uncheck those not in the original
                    var checkableFolder = new CheckableTreeNode<Folder>(new Folder(folder.FullPath));
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
                ImportFolderWizard importFolderWindow = new(this)
                {
                    Owner = Application.Current.MainWindow
                };

                var dialogResult = importFolderWindow.ShowDialog();
                if (dialogResult == false) return new();
            }

            //filer toImport so it only contains files from checked subfolders
            if (CheckableFolders.Any())
            {
                List<string> toImportBySubFolders = new();
                var checkedFolders = CheckableTreeNode<Folder>.GetCheckedItems(CheckableFolders).Flatten();
                foreach (var folder in checkedFolders)
                {
                    toImportBySubFolders.AddRange(Directory.GetFiles(folder.FullPath));
                }
                toImport = toImport.Intersect(toImportBySubFolders).ToList();
            }

            //filter out File types and return
            return toImport.Where(path => _targetCollection.Info.FiletypePreferences[Path.GetExtension(path)]).ToList();
        }

        #region Subfolder Select Step
        public int ImportAmount { get; set; }

        public List<CheckableTreeNode<Folder>> CheckableFolders { get; set; } = new();
        #endregion

        #region File Type Selection Step
        private IEnumerable<FileTypeInfo> _knownFileTypes = Enumerable.Empty<FileTypeInfo>();
        public IEnumerable<FileTypeInfo> KnownFileTypes
        {
            get => _knownFileTypes;
            set => SetProperty(ref _knownFileTypes, value);
        }

        private IEnumerable<FileTypeInfo> _unknownFileTypes = Enumerable.Empty<FileTypeInfo>();
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

        public override Task ApplyAll()
        {
            //Update the Auto Import Folders
            if (AddAutoImportFolders)
            {
                //go over every folder and set the HasAllSubFolder Flag
                foreach (var checkableFolder in CheckableFolders.Flatten())
                {
                    checkableFolder.Item.HasAllSubFolders = checkableFolder.IsChecked == true;
                }

                //Add the folder to the AutoImportFolders
                var checkedFolders = CheckableTreeNode<Folder>.GetCheckedItems(CheckableFolders);
                foreach (Folder folder in checkedFolders)
                {
                    _targetCollection.Info.AutoImportFolders.AddIfMissing(folder);
                }

                //Remove the existingFolders as they have been replaced
                foreach (Folder folder in ExistingFolders)
                {
                    _targetCollection.Info.AutoImportFolders.Remove(folder);
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
