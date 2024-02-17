using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using Ookii.Dialogs.Wpf;
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

        public List<string> RecursiveFolders { get; set; } = new();
        public List<string> NonRecursiveFolders { get; set; } = new();
        public List<string> Files { get; set; } = new();


        private bool _autoImportFolders = true;
        public bool AddAutoImportFolders
        {
            get => _autoImportFolders;
            set => SetProperty(ref _autoImportFolders, value);
        }

        public async Task Import()
        {
            //if no files are given to import, don't
            if (_manuallyTriggered && RecursiveFolders.Count + NonRecursiveFolders.Count + Files.Count == 0)
            {
                LetUserSelectFolders();
            }

            var toImport = GetPathsToImport();
            toImport = LetUserFilterToImport(toImport);
            await ImportViewModel.ImportFilesAsync(toImport, _targetCollection);
        }

        /// <summary>
        /// Lets a user select folders using a dialog 
        /// and stores them in RecursiveFolders
        /// </summary>
        /// <returns>A list of paths </returns>
        private void LetUserSelectFolders()
        {
            VistaFolderBrowserDialog openFolderDialog = new()
            {
                Multiselect = true,
            };

            var dialogResult = openFolderDialog.ShowDialog();
            if (dialogResult == false) return;

            RecursiveFolders = openFolderDialog.SelectedPaths.ToList();
        }

        /// <summary>
        /// Get a list of all the file paths that are not banned
        /// because of banishent or due to file extension preference
        /// That are either in FileName or in a folder in RecursiveFolders
        /// </summary>
        /// <returns></returns>
        private List<string> GetPathsToImport()
        {
            // 1. Unroll the recursive folders and add them to non recursive folders
            List<string> toSearch = new(RecursiveFolders);
            while (toSearch.Any())
            {
                string currentFolder = toSearch[0];
                NonRecursiveFolders.Add(currentFolder);
                toSearch.AddRange(Directory.GetDirectories(currentFolder));
                toSearch.Remove(currentFolder);
            }

            //2. Build a list with all the files to import
            List<string> toImport = new(Files);
            foreach (var folder in NonRecursiveFolders)
            {
                toImport.AddRange(Directory.GetFiles(folder));
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
                IEnumerable<Folder> folderObjects = RecursiveFolders.Select(f => new Folder(f));
                CheckableFolders = folderObjects.Select(f => new CheckableTreeNode<Folder>(f)).ToList();
            }

            //find how many files of each filetype
            var toImportGrouped = toImport.GroupBy(Path.GetExtension).ToList();
            var extensions = toImportGrouped.Select(x => x.Key).ToList();
            var newExtensions = extensions.Except(_targetCollection.Info.FiletypePreferences.Keys).ToList();

            //Add Extionsions Step
            if (_manuallyTriggered || newExtensions.Any())
            {
                Steps.Add("Extensions");

                KnownFileTypes = toImportGrouped
                    .Where(grouping => _targetCollection.Info.FiletypePreferences.Keys.Contains(grouping.Key))
                    .Select(x => new FileTypeInfo(x.Key, _targetCollection.Info.FiletypePreferences[x.Key], x.Count())).ToList();

                UnknownFileTypes = toImportGrouped
                    .Where(grouping => !_targetCollection.Info.FiletypePreferences.Keys.Contains(grouping.Key))
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

            //filer toImport so it only contains files from check subfolders
            if (CheckableFolders != null)
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
        private IEnumerable<FileTypeInfo> _knownFileTypes;
        public IEnumerable<FileTypeInfo> KnownFileTypes
        {
            get => _knownFileTypes;
            set => SetProperty(ref _knownFileTypes, value);
        }

        private IEnumerable<FileTypeInfo> _unknownFileTypes;
        public IEnumerable<FileTypeInfo> UnknownFileTypes
        {
            get => _unknownFileTypes;
            set => SetProperty(ref _unknownFileTypes, value);
        }
        public IEnumerable<FileTypeInfo> ToImportFiletypes => UnknownFileTypes.Concat(KnownFileTypes);


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
                var checkedFolders = CheckableTreeNode<Folder>.GetCheckedItems(CheckableFolders);
                foreach (Folder folder in checkedFolders)
                {
                    _targetCollection.Info.AutoImportFolders.AddIfMissing(folder);
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
