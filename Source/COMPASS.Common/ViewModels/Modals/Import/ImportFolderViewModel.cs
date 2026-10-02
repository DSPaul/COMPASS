using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Infra.Avalonia.Mvvm;
using COMPASS.Infra.Collections;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Selection;

namespace COMPASS.Common.ViewModels.Modals.Import
{
    /// <summary>
    /// Viewmodel for importing files and folders into a collection.
    /// </summary>
    public sealed class ImportFolderViewModel : ViewModelBase, IModalViewModel, IConfirmable
    {
        private readonly ILogger _logger;
        private readonly FolderFactory _folderFactory;
        private readonly bool _isAutoImport;
        private readonly CollectionInfo _collectionInfo;
        private readonly List<string> _allFiles;

        public bool Finished { get; private set; }

        public ImportFolderViewModel(
            ILogger logger,
            FolderFactory folderFactory,
            bool isAutoImport,
            CollectionInfo collectionInfo,
            IList<Folder> folders,
            IList<string> allFiles)
        {
            _logger = logger;
            _folderFactory = folderFactory;
            _isAutoImport = isAutoImport;
            _collectionInfo = collectionInfo;
            _allFiles = allFiles.ToList();

            IncludeSubfolders = true;
            //make sure there are no duplicates (normalized: separators, trailing slashes; comparer: casing)
            List<Folder> distinctFolders = folders.DistinctBy(f => PathUtils.NormalizePath(f.FullPath), PathUtils.PathComparer).ToList();

            //Get the root folders with all subfolders on disk to show for exclusion
            List<Folder> rootFolders = distinctFolders.Select(folder => folderFactory.Create(folder.FullPath)).ToList();
            SelectSubfoldersVM = new(rootFolders);

            //Existing folders may already have a list of explicit subfolders,
            //which may be a subset of the subfolders on disk
            //so we need to check the subfolders that are already in the collection
            foreach (CheckableTreeNode<Folder> rootNode in SelectSubfoldersVM.OptionsRoot)
            {
                rootNode.IsChecked = true;

                Folder originalFolder = distinctFolders.Single(folder => PathUtils.PathsEqual(folder.FullPath, rootNode.Item.FullPath));

                HashSet<string> chosenSubFolderPaths = originalFolder.SubFolders.Flatten().Select(subFolder => subFolder.FullPath).ToHashSet();
                foreach (CheckableTreeNode<Folder> subFolderNode in rootNode.Children.Flatten())
                {
                    subFolderNode.IsChecked = chosenSubFolderPaths.Contains(subFolderNode.Item.FullPath);
                }

                rootNode.Updated += (_, _) => RefreshFilteredFileCount();
            }

            //find how many files of each filetype
            KnownFileTypes = new List<FileTypeInfo>();
            UnknownFileTypes = new List<FileTypeInfo>();
            InitFileTypeInfos();

            RefreshFilteredFileCount();
        }

        private void InitFileTypeInfos()
        {
            List<IGrouping<string, string>> filesByExtension = _allFiles.GroupBy<string, string>(Path.GetExtension).ToList();

            foreach (var extensionGroup in filesByExtension)
            {
                bool known = _collectionInfo.FiletypePreferences.TryGetValue(extensionGroup.Key, out bool storedShouldImport);
                bool shouldImport = known ? storedShouldImport : true;
                FileTypeInfo extensionInfo = new FileTypeInfo(extensionGroup.Key, shouldImport, extensionGroup.Count());

                extensionInfo.PropertyChanged += (_, _) => RefreshFilteredFileCount();

                if (known)
                {
                    KnownFileTypes.Add(extensionInfo);
                }
                else
                {
                    UnknownFileTypes.Add(extensionInfo);
                }
            }

            //A stored exclusion  should stick: enable exclude when an known file type is exluced
            _excludeFileTypes = KnownFileTypes.Any(fileType => !fileType.ShouldImport);
        }

        #region IModalViewModel

        public string WindowTitle => _isAutoImport ? "AutoImport" : "Import Folder(s)";
        public Action CloseAction { get; set; } = () => { };

        #endregion

        #region IConfirmable

        public IRelayCommand ConfirmCommand => field ??= new RelayCommand(Confirm);
        public IRelayCommand CancelCommand => field ??= new RelayCommand(CloseAction);

        #endregion

        /// <summary>
        /// Always show dialog on manual import, only when decistion must be made about new file types in case of auto import
        /// </summary>
        public bool ShouldShowDialog => !_isAutoImport || UnknownFileTypes.Count > 0;

        public bool AddAutoImportFolders
        {
            get;
            set => SetProperty(ref field, value);
        } = true;

        #region Subfolder inclusion

        public HierarchicalSelectorViewModel<Folder>? SelectSubfoldersVM { get; }

        public bool IncludeSubfolders
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    RefreshFilteredFileCount();
                }
            }
        }

        /// <summary>
        /// False when there are no subfolders to choose from,
        /// in which case the whole subfolder section is hidden.
        /// </summary>
        public bool HasSubfolderChoices => SelectSubfoldersVM is not null
                                           && SelectSubfoldersVM.TotalOptionsCount > SelectSubfoldersVM.OptionsRoot.Count;

        public string SubfoldersCheckBoxText
        {
            get
            {
                int subfolderCount = (SelectSubfoldersVM?.TotalOptionsCount ?? 0) - (SelectSubfoldersVM?.OptionsRoot.Count ?? 0);
                return $"Include subfolders ({subfolderCount} found)";
            }
        }

        #endregion

        #region File type exclusion

        private bool _excludeFileTypes;
        public bool ExcludeFileTypes
        {
            get => _excludeFileTypes;
            set
            {
                if (SetProperty(ref _excludeFileTypes, value))
                {
                    OnPropertyChanged(nameof(ShowNewFileTypesHint));
                    RefreshFilteredFileCount();
                }
            }
        }

        public IList<FileTypeInfo> KnownFileTypes { get; }
        public IList<FileTypeInfo> UnknownFileTypes { get; }

        public bool HasNewFileTypes => UnknownFileTypes.Count > 0;
        public bool ShowNewFileTypesHint => HasNewFileTypes && !ExcludeFileTypes;

        public string NewFileTypesHint
        {
            get
            {
                string newExtensions = string.Join(", ", UnknownFileTypes.Select(fileType => fileType.FileExtension));
                return UnknownFileTypes.Count == 1
                    ? $"New file type found: {newExtensions}."
                    : $"New file types found: {newExtensions}.";
            }
        }

        //helper class for file type selection during folder import
        public sealed class FileTypeInfo : ObservableObject
        {
            public FileTypeInfo(string extension, bool shouldImport, int fileCount = 0)
            {
                FileExtension = extension;
                FileCount = fileCount;
                _shouldImport = shouldImport;
            }

            public string FileExtension { get; }
            public int FileCount { get; }

            private bool _shouldImport;
            public bool ShouldImport
            {
                get => _shouldImport;
                set => SetProperty(ref _shouldImport, value);
            }

            public string DisplayText => $"{FileExtension} ({FileCount} file{(FileCount == 1 ? "" : "s")})";
        }

        #endregion

        #region File counts

        public int TotalFilesCount => _allFiles.Count;

        private int _filesToImportCount;
        public int FilesToImportCount
        {
            get => _filesToImportCount;
            private set
            {
                if (SetProperty(ref _filesToImportCount, value))
                {
                    OnPropertyChanged(nameof(FilesToImportSummary));
                    OnPropertyChanged(nameof(ConfirmButtonText));
                }
            }
        }

        public string FilesFoundSummary => $"{TotalFilesCount} file{(TotalFilesCount == 1 ? "" : "s")} found";
        public string FilesToImportSummary => $"Importing {FilesToImportCount}/{TotalFilesCount}";

        public string ConfirmButtonText => FilesToImportCount == 1
            ? "Import (1 file)"
            : $"Import ({FilesToImportCount} files)";

        public string UnknownTypesFoundSummary => $"{UnknownFileTypes.Count} new file extensions ({UnknownFileTypes.Count(t => !t.ShouldImport)} excluded)";
        public string KnownTypesFoundSummary => $"{KnownFileTypes.Count} known file extensions ({KnownFileTypes.Count(t => !t.ShouldImport)} excluded)";

        private void RefreshFilteredFileCount()
        {
            FilesToImportCount = GetFilteredFiles().Count;
            OnPropertyChanged(nameof(KnownTypesFoundSummary));
            OnPropertyChanged(nameof(UnknownTypesFoundSummary));
        }

        #endregion

        /// <summary>
        /// Filters the full file list using the current dialog choices.
        /// </summary>
        public IList<string> GetFilteredFiles() => GetFilteredFiles(_allFiles);

        /// <summary>
        /// Filters a file list using the current dialog choices.
        /// File type exclusion only happens while file type exclusion is enabled.
        /// </summary>
        public IList<string> GetFilteredFiles(IList<string> filesToFilter)
        {
            IList<string> filteredFiles = [.. filesToFilter];
            if (IncludeSubfolders)
            {
                filteredFiles = FilterFilesBySubFolders(filteredFiles);
            }
            else
            {
                filteredFiles = FilterTopLevelFilesOnly(filteredFiles);
            }

            if (ExcludeFileTypes)
            {
                filteredFiles = FilterFilesByExtension(filteredFiles);
            }
            return filteredFiles;
        }

        private List<string> FilterFilesBySubFolders(IList<string> files)
        {
            //filter files so that it only contains those from checked subfolders
            //because there may also be loose files in the list
            //we need to remove those in unchecked folders, rather than include those in checked folders
            IList<Folder> excludedFolders = SelectSubfoldersVM?.UncheckedOptions ?? [];
            List<string> excludedBySubfolder = files.Where(path => excludedFolders.Any(folder => PathUtils.IsPathInsideDirectory(path, folder.FullPath))).ToList();
            return files.Except(excludedBySubfolder).ToList();
        }

        private IList<string> FilterTopLevelFilesOnly(IList<string> files)
        {
            if(SelectSubfoldersVM == null)
            {
                return files;
            }

            //Only keep top level files (aka not in subfolder)
            return files.Where(f => 
                SelectSubfoldersVM.OptionsRoot.All(d => !PathUtils.IsPathInsideDirectory(f,d.Item.FullPath)) || //Either not in any of the folders => loose file (possible when drag and dropping a mix of foldes and files)
                SelectSubfoldersVM.OptionsRoot.Any(d => PathUtils.PathsEqual(d.Item.FullPath, Path.GetDirectoryName(f)))).ToList(); //
        }

        private List<string> FilterFilesByExtension(IList<string> files)
        {
            Dictionary<string, bool> effectivePreferences = new(_collectionInfo.FiletypePreferences);
            foreach (FileTypeInfo fileTypeInfo in KnownFileTypes.Concat(UnknownFileTypes))
            {
                effectivePreferences[fileTypeInfo.FileExtension] = fileTypeInfo.ShouldImport;
            }

            return files.Where(path => !effectivePreferences.TryGetValue(Path.GetExtension(path), out bool shouldImport) || shouldImport).ToList();
        }

        private void Confirm()
        {
            //Update the Auto Import Folders
            if (AddAutoImportFolders && SelectSubfoldersVM is not null)
            {
                //go over every folder and update the original if any, otherwise add it
                foreach (CheckableTreeNode<Folder> checkableFolder in SelectSubfoldersVM.OptionsRoot.Flatten())
                {
                    bool allSubfoldersChecked = checkableFolder.Children.All(child => child.IsChecked != false);
                    if (allSubfoldersChecked)
                    {
                        checkableFolder.Item.ClearExplicitSubfolders();
                    }
                    else
                    {
                        List<Folder> explicitSubfolders = checkableFolder.Children
                            .Where(child => child.IsChecked != false)
                            .Select(child => child.Item)
                            .ToList();
                        checkableFolder.Item.SetExplicitSubfolders(explicitSubfolders);
                    }

                    //If folder was already in auto import, remove it so it can be replaced with the new version
                    _collectionInfo.AutoImportFolders.RemoveWhere(folder => PathUtils.PathsEqual(folder.FullPath, checkableFolder.Item.FullPath));
                }

                //Now add the top level folders to the auto import list
                List<Folder> checkedFolders = SelectSubfoldersVM.OptionsRoot.Where(root => root.IsChecked != false).Select(root => root.Item).ToList();
                foreach (Folder folder in checkedFolders)
                {
                    //this could be a subfolder of a folder already in autoImport
                    //check upwards to see if any parent folder is already in autoImport
                    Folder? foundParent = null;

                    Stack<string> checkedDirectories = [];
                    checkedDirectories.Push(folder.FullPath);

                    foreach (string parentFolder in PathUtils.GetAllParentDirectories(folder.FullPath))
                    {
                        if (_collectionInfo.AutoImportFolders.FirstOrDefault(autoImportFolder => PathUtils.PathsEqual(autoImportFolder.FullPath, parentFolder)) is { } parent)
                        {
                            foundParent = parent;
                            break;
                        }
                        checkedDirectories.Push(parentFolder);
                    }

                    if (foundParent != null) //there is a parent folder of 'folder' already in autoimport
                    {
                        //go back down the tree, checking all necessary subfolders along the way
                        while (checkedDirectories.TryPop(out string? parentFolderPath))
                        {
                            foundParent.UpdateAllSubFolders();
                            Folder? lowerParent = foundParent.SubFolders.SingleOrDefault(subFolder => PathUtils.PathsEqual(subFolder.FullPath, parentFolderPath));

                            //if not an included subfolder already, take it from allSubFolders and add it to subfolders
                            if (lowerParent == null)
                            {
                                lowerParent = foundParent.AllSubFolders.SingleOrDefault(subFolder => PathUtils.PathsEqual(subFolder.FullPath, parentFolderPath));

                                if (lowerParent == null)
                                {
                                    //not in allSubFolders which was just refreshed so not on disk
                                    //If parent of "folder' is not on disk, then neither is 'folder'
                                    //So just blow off the whole thing
                                    _logger.Warn($"Folder {parentFolderPath} was not found, so it will not be added to auto import");
                                    lowerParent = _folderFactory.Create(parentFolderPath);
                                }

                                foundParent.SetExplicitSubfolders([.. foundParent.SubFolders, lowerParent]);
                                //If not yet target folder, explicit empty subfolders to not accidentally add other folders
                                if (!PathUtils.PathsEqual(lowerParent.FullPath, folder.FullPath))
                                {
                                    lowerParent.SetExplicitSubfolders([]);
                                }
                            }

                            //if reached target, copy over explicit subfolders
                            if (PathUtils.PathsEqual(lowerParent.FullPath, folder.FullPath))
                            {
                                if (folder.HasAllSubFolders)
                                {
                                    lowerParent.ClearExplicitSubfolders();
                                }
                                else
                                {
                                    lowerParent.SetExplicitSubfolders(folder.SubFolders);
                                }
                            }

                            foundParent = lowerParent;
                        }
                    }
                    else
                    {
                        _collectionInfo.AutoImportFolders.Add(folder);
                    }
                }
            }

            //update the collections file type preferences
            foreach (FileTypeInfo fileTypeInfo in UnknownFileTypes)
            {
                _collectionInfo.FiletypePreferences.TryAdd(fileTypeInfo.FileExtension, fileTypeInfo.ShouldImport);
            }
            foreach (FileTypeInfo fileTypeInfo in KnownFileTypes)
            {
                _collectionInfo.FiletypePreferences[fileTypeInfo.FileExtension] = fileTypeInfo.ShouldImport;
            }

            Finished = true;
            CloseAction();
        }
    }

    [Factory]
    public class ImportFolderViewModelFactory(ILogger logger, FolderFactory folderFactory)
    {
        public ImportFolderViewModel Create(
            bool isAutoImport,
            CollectionInfo collectionInfo,
            IList<Folder> folders,
            IList<string> allFiles)
        {
            return new ImportFolderViewModel(logger, folderFactory, isAutoImport, collectionInfo, folders, allFiles);
        }
    }
}
