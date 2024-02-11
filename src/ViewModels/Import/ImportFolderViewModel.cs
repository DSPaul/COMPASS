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

        public List<string> FolderNames { get; set; } = new();
        public List<string> FileNames { get; set; } = new();


        private bool _autoImportFolders = true;
        public bool AutoImportFolders
        {
            get => _autoImportFolders;
            set => SetProperty(ref _autoImportFolders, value);
        }

        /// <summary>
        /// Lets a user select folders using a dialog 
        /// and stores them in FoldernNames
        /// </summary>
        /// <returns></returns>
        public List<string> LetUserSelectFolders()
        {
            VistaFolderBrowserDialog openFolderDialog = new()
            {
                Multiselect = true,
            };

            var dialogResult = openFolderDialog.ShowDialog();
            if (dialogResult == false) return new();

            return FolderNames = openFolderDialog.SelectedPaths.ToList();
        }

        /// <summary>
        /// Get a list of all the file paths that are not banned
        /// because of banishent or due to file extension preference
        /// That are either in FileName or in a folder in FolderNames
        /// </summary>
        /// <returns></returns>
        public List<string> GetPathsFromFolders()
        {
            //find files in folder, including subfolder
            List<string> toSearch = new(FolderNames); //list with folders to search
            List<string> toImport = new(FileNames); //list with files to import

            //Find all files in the subfolders
            while (toSearch.Count > 0)
            {
                string currentFolder = toSearch[0];
                toSearch.AddRange(Directory.GetDirectories(currentFolder));
                toImport.AddRange(Directory.GetFiles(currentFolder));
                toSearch.Remove(currentFolder);
            }

            //Filter out banished paths
            toImport = toImport.Except(_targetCollection.Info.BanishedPaths).ToList();

            //find how many files of each filetype
            var toImportGrouped = toImport.GroupBy(Path.GetExtension).ToList();
            var extensions = toImportGrouped.Select(x => x.Key).ToList();
            var newExtensions = extensions.Except(_targetCollection.Info.FiletypePreferences.Keys).ToList();

            //Add SubFolders Step
            if (_manuallyTriggered)
            {
                Steps.Add("SubFolders");

                ImportAmount = toImport.Count;

                //Build the checkable folder Tree
                IEnumerable<Folder> folderObjects = FolderNames.Select(f => new Folder(f));
                CheckableFolders = folderObjects.Select(f => new CheckableTreeNode<Folder>(f)).ToList();
            }

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

        public List<CheckableTreeNode<Folder>> CheckableFolders { get; set; }
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
            //update the global file type preferences for the collection
            foreach (var filetypeHelper in UnknownFileTypes)
            {
                _targetCollection.Info.FiletypePreferences.TryAdd(filetypeHelper.FileExtension, filetypeHelper.ShouldImport);
            }
            foreach (var filetypeHelper in UnknownFileTypes)
            {
                _targetCollection.Info.FiletypePreferences[filetypeHelper.FileExtension] = filetypeHelper.ShouldImport;
            }

            //Update the Auto Import Folder
            //TODO exclude subfolders
            if (AutoImportFolders)
            {
                foreach (string dir in FolderNames)
                {
                    _targetCollection.Info.AutoImportDirectories.AddIfMissing(dir);
                }
            }

            CloseAction?.Invoke();
            return Task.CompletedTask;
        }
    }
}
