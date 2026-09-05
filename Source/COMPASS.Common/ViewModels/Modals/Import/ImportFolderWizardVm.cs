using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Models;
using COMPASS.Common.ViewModels.Selection;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools;
using ExCSS;

namespace COMPASS.Common.ViewModels.Modals.Import
{
    public sealed class ImportFolderWizardVm : WizardViewModel
    {
        private readonly FolderFactory _folderFactory;
        private readonly bool _autoImport;
        private readonly CollectionInfo _collectionInfo;

        public bool Finished { get; private set; }

        public ImportFolderWizardVm(
            FolderFactory folderFactory,
            bool autoImport,
            CollectionInfo collectionInfo,
            IList<Folder> folders,
            IList<string> allFiles)
        {
            _folderFactory = folderFactory;
            _autoImport = autoImport;
            _collectionInfo = collectionInfo;

            //Add SubFolders Step
            if (!_autoImport)
            {
                //TODO: this step isn't really needed if there are no subfolders
                //except for auto import checkbox, could probably just check that behind the scenes
                Steps.Add(_subFoldersStep);

                //Get the root folders with all subfolders on disk to show in step
                var rootFolders = folders.Select(f => folderFactory.Create(f.FullPath)).ToList();
                SelectSubfoldersVM = new(rootFolders);

                //Existing folders may already have a list of explicit subfolders,
                //which may be a subset of the subfolders on disk
                //so we need to check the subfolders that are already in the collection
                foreach (var node in SelectSubfoldersVM.OptionsRoot)
                {
                    node.IsChecked = true;

                    Folder? origFolder = folders.Single(f => f.FullPath == node.Item.FullPath);

                    var chosenSubFolderPaths = origFolder.SubFolders.Flatten().Select(sf => sf.FullPath).ToHashSet();
                    foreach (var subNode in node.Children.Flatten())
                    {
                        subNode.IsChecked = chosenSubFolderPaths.Contains(subNode.Item.FullPath);
                    }
                }
            }

            //find how many files of each filetype
            var toImportGrouped = allFiles.GroupBy<string, string>(Path.GetExtension).ToList();
            var extensions = toImportGrouped.Select(x => x.Key).ToList();
            var newExtensions = extensions.Except(_collectionInfo.FiletypePreferences.Keys).ToList();

            //Add Extensions Step
            //TODO this could be updated whenever a folder is checked or unchecked
            if (newExtensions.Any())
            {
                Steps.Add(_extensionsStep);

                KnownFileTypes = toImportGrouped
                    .Where(grouping => _collectionInfo.FiletypePreferences.ContainsKey(grouping.Key))
                    .Select(x => new FileTypeInfo(x.Key, _collectionInfo.FiletypePreferences[x.Key], x.Count())).ToList();

                UnknownFileTypes = toImportGrouped
                    .Where(grouping => !_collectionInfo.FiletypePreferences.ContainsKey(grouping.Key))
                    .Select(x => new FileTypeInfo(x.Key, true, x.Count())).ToList();
            }
        }

        private readonly WizardStepViewModel _subFoldersStep = new("Choose which subfolders to import", "SubFolders");
        private readonly WizardStepViewModel _extensionsStep = new("Choose which file types to import", "Extensions");

        #region IModalWindow

        public override string WindowTitle => _autoImport ? "AutoImport" : "Import Folder(s)";

        #endregion

        private bool _addAutoImportFolders = true;
        public bool AddAutoImportFolders
        {
            get => _addAutoImportFolders;
            set => SetProperty(ref _addAutoImportFolders, value);
        }

        #region Subfolder Select Step

        public HierarchicalSelectorViewModel<Folder>? SelectSubfoldersVM { get; set; }

        #endregion

        #region File Type Selection Step
        private IList<FileTypeInfo> _knownFileTypes = [];
        public IList<FileTypeInfo> KnownFileTypes
        {
            get => _knownFileTypes;
            set => SetProperty(ref _knownFileTypes, value);
        }

        private IList<FileTypeInfo> _unknownFileTypes = [];
        public IList<FileTypeInfo> UnknownFileTypes
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

        /// <summary>
        /// Shows an ImportFolderWizard if certain conditions are met
        /// </summary>
        /// <param name="toImport"></param>
        /// <returns></returns>
        public List<string> GetFilteredFiles(IList<string> toImport)
        {
            List<string> filteredList = [.. toImport];
            filteredList = FilterFilesBySubFolders(filteredList);
            return FilterFilesByExtension(filteredList);
        }

        private List<string> FilterFilesBySubFolders(IList<string> files)
        {
            //filter files so that it only contains those from checked subfolders
            //because there may also be loose files in the list
            //we need to remove those in unchecked folders, rather than include those in checked folders

            IList<Folder> excludedFolders = SelectSubfoldersVM?.UncheckedOptions ?? [];
            var excludedBySubfolder = files.Where(path => excludedFolders.Any(folder => Path.GetDirectoryName(path) == folder.FullPath));
            return files.Except(excludedBySubfolder).ToList();
        }

        private List<string> FilterFilesByExtension(IList<string> files)
        {
            return files.Where(path => _collectionInfo.FiletypePreferences[Path.GetExtension(path)]).ToList();
        }

        public override Task Finish()
        {
            //Update the Auto Import Folders
            if (AddAutoImportFolders)
            {
                //go over every folder and updat the original if any, other wise add it
                foreach (var checkableFolder in SelectSubfoldersVM?.OptionsRoot.Flatten() ?? [])
                {
                    bool allSubfoldersChecked = checkableFolder.Children.All(child => child.IsChecked != false);
                    if (allSubfoldersChecked)
                    {
                        checkableFolder.Item.ClearExplicitSubfolders();
                    }
                    else
                    {
                        var explicitSubfolders = checkableFolder.Children
                            .Where(child => child.IsChecked != false)
                            .Select(child => child.Item)
                            .ToList();
                        checkableFolder.Item.SetExplicitSubfolders(explicitSubfolders);
                    }

                    //If folder was already in auto import, remove it so it can be replaced with the new version
                    _collectionInfo.AutoImportFolders.RemoveWhere(f => f.FullPath == checkableFolder.Item.FullPath);
                }

                //Now add the top level folders to the auto import list
                var checkedFolders = SelectSubfoldersVM?.OptionsRoot.Where(x => x.IsChecked != false).Select(x => x.Item).ToList() ?? [];
                foreach (Folder folder in checkedFolders)
                {
                    //this could be a subfolder of a folder already in autoImport
                    //check upwards to see if any parent folder is already in autoImport
                    Folder? foundParent = null;

                    Stack<string> checkedDirectories = [];
                    checkedDirectories.Push(folder.FullPath);

                    foreach(var parentFolder in PathUtils.GetAllParentDirectories(folder.FullPath))
                    {
                        if (_collectionInfo.AutoImportFolders.FirstOrDefault(f => f.FullPath == parentFolder) is { } parent)
                        {
                            foundParent = parent;
                            break;
                        }
                        checkedDirectories.Push(parentFolder);
                    }

                    if (foundParent != null)
                    {
                        //go back down the tree, checking all neccessary subfolders along the way
                        while (checkedDirectories.TryPop(out var parentFolder)) 
                        {
                            foundParent.UpdateAllSubFolders(_folderFactory);
                            var lowerParent = foundParent.SubFolders.SingleOrDefault(sf => sf.FullPath == parentFolder);
                            
                            //if not a subfolders already, take it from allSubFolders and add it
                            if (lowerParent == null)
                            {
                                lowerParent = foundParent.AllSubFolders.Single(p => p.FullPath == parentFolder);
                                foundParent.SetExplicitSubfolders([.. foundParent.SubFolders, lowerParent]);
                                //If not yet target folder, explicit empty subfolders to not accidentally add other folders
                                if(lowerParent.FullPath != folder.FullPath)
                                {
                                    lowerParent.SetExplicitSubfolders([]);
                                }
                            }

                            //if reached target, copy over explicit subfolders
                            if (lowerParent.FullPath == folder.FullPath) 
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
            foreach (var filetypeHelper in UnknownFileTypes)
            {
                _collectionInfo.FiletypePreferences.TryAdd(filetypeHelper.FileExtension, filetypeHelper.ShouldImport);
            }
            foreach (var filetypeHelper in KnownFileTypes)
            {
                _collectionInfo.FiletypePreferences[filetypeHelper.FileExtension] = filetypeHelper.ShouldImport;
            }

            Finished = true;
            CloseAction();
            return Task.CompletedTask;
        }
    }

    [Factory]
    public class ImportFolderWizardFactory(FolderFactory folderFactory)
    {
        public ImportFolderWizardVm Create(
            bool autoImport,
            CollectionInfo collectionInfo,
            IList<Folder> folders,
            IList<string> allFiles)
        {
            return new ImportFolderWizardVm(folderFactory, autoImport, collectionInfo, folders, allFiles);
        }
    }
}
