using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Selection;

namespace COMPASS.Common.ViewModels.Modals.Import
{
    public sealed class ImportFolderWizardVm : WizardViewModel
    {
        private readonly bool _autoImport;
        private readonly CollectionInfo _collectionInfo;

        public bool Finished { get; private set; }
        
        public ImportFolderWizardVm(
            bool autoImport, 
            CollectionInfo collectionInfo, 
            IList<Folder> folders, 
            IList<string> allFiles)
        {
            _autoImport = autoImport;
            _collectionInfo = collectionInfo;
            
            //Add SubFolders Step
            if (!_autoImport)
            {
                //TODO: this step isn't really needed if there are no subfolders
                //except for auto import checkbox, could probably just check that behind the scenes
                Steps.Add(_subFoldersStep);
                SelectSubfoldersVM = new(folders);

                //Existing folders already have a list of subfolders, which may be a subset of the subfolders on disk
                //so we need to check the subfolders that are already in the collection
                foreach (var node in SelectSubfoldersVM.OptionsRoot)
                {
                    var chosenSubFolderPaths = node.Item.SubFolders.Flatten().Select(sf => sf.FullPath).ToList();
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
        
        public override string WindowTitle => _autoImport ?  "AutoImport" : "Import Folder(s)";
        public override int? WindowWidth => 600;
        public override int? WindowHeight => 400;
        
        #endregion

        private bool _addAutoImportFolders = true;
        public bool AddAutoImportFolders
        {
            get => _addAutoImportFolders;
            set => SetProperty(ref _addAutoImportFolders, value);
        }

        #region Subfolder Select Step
        
        public HierarchicalSelectorViewmodel<Folder>? SelectSubfoldersVM { get; set; }
        
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
            List<string> filteredList = [..toImport];
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
        
        protected override Task Finish()
        {
            //Update the Auto Import Folders
            if (AddAutoImportFolders)
            {
                //go over every folder and set the HasAllSubFolder Flag
                foreach (var checkableFolder in SelectSubfoldersVM?.OptionsRoot.Flatten() ?? [])
                {
                    checkableFolder.Item.HasAllSubFolders =
                        checkableFolder.IsChecked == true &&
                        checkableFolder.Children.All(child => child.IsChecked == true); //need this check as well because folder might also be checked without any children
                }

                //Remove the existingFolders as they will be replaced
                var updatedFolders = SelectSubfoldersVM?.SelectedOptions ?? [];
                
                foreach (Folder folder in updatedFolders)
                {
                    _collectionInfo.AutoImportFolders.Remove(folder);
                }

                //Add the folder to the AutoImportFolders
                foreach (Folder folder in updatedFolders)
                {
                    if (_collectionInfo.AutoImportFolders.All(f => f.FullPath != folder.FullPath))
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
}
