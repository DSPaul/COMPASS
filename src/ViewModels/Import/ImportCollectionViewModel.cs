using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace COMPASS.ViewModels.Import
{
    public class ImportCollectionViewModel : WizardViewModel
    {
        public ImportCollectionViewModel(CodexCollection collectionToImport)
        {
            CollectionToImport = collectionToImport;
            CollectionToImport.Load(true);

            //Checks which steps need to be included in wizard
            HasCodices = CollectionToImport.AllCodices.Any();
            HasTags = CollectionToImport.AllTags.Any();
            HasSettings = CollectionToImport.Info.ContainsSettings();
            UpdateSteps();

            //Put Tags in Checkable Wrapper
            TagsToImport = CollectionToImport.RootTags.Select(t => new CheckableTreeNode<Tag>(t)).ToList();

            //Put codices in dictionary so they can be labeled true/false for import
            CodicesToImport = CollectionToImport.AllCodices.Select(codex => new ImportCodexHelper(codex)).ToList();

            //prep settings data for selection
            AutoImportFolders = CollectionToImport.Info.AutoImportDirectories.Select(folder => new ImportPathHelper(folder)).ToList();
            BanishedPaths = CollectionToImport.Info.BanishedPaths.Select(path => new ImportPathHelper(path)).ToList();
            FileTypePrefs = CollectionToImport.Info.FiletypePreferences
                                                            .Select(x => new ObservableKeyValuePair<string, bool>(x))
                                                            .OrderByDescending(x => x.Value)
                                                            .ToList();
            FolderTagLinks = CollectionToImport.Info.FolderTagPairs
                .Select(link => new ImportFolderTagLinkHelper(link.Folder, link.Tag, Utils.FlattenTree(CheckableTreeNode<Tag>.GetCheckedItems(TagsToImport))))
                .ToList();

            //if files were included in compass file, set paths of codices to those files
            if (Directory.Exists(CollectionToImport.UserFilesPath))
            {
                foreach (Codex codex in CollectionToImport.AllCodices)
                {
                    string includedFilePath = Path.Combine(CollectionToImport.UserFilesPath, Path.GetFileName(codex.Path));
                    if (File.Exists(includedFilePath))
                    {
                        codex.Path = includedFilePath;
                    }
                }
            }
        }

        public CodexCollection TargetCollection { get; set; } = null; //null means new collection should be made
        public CodexCollection CollectionToImport { get; set; } = null; //collection that was in the cmpss file

        //OVERVIEW STEP
        public bool MergeIntoCollection { get; set; } = true;
        public string CollectionName { get; set; } = "Unnamed Collection";

        public bool HasTags { get; set; }
        public bool HasCodices { get; set; }
        public bool HasSettings { get; set; }

        public bool ImportAllTags { get; set; } = true;
        public bool ImportAllCodices { get; set; } = true;
        public bool ImportAllSettings { get; set; } = true;

        private bool _advancedImport = false;
        public bool AdvancedImport
        {
            get => _advancedImport;
            set
            {
                SetProperty(ref _advancedImport, value);
                UpdateSteps();
            }
        }

        //TAGS STEP
        public List<CheckableTreeNode<Tag>> TagsToImport { get; set; }

        // CODICES STEP
        public List<ImportCodexHelper> CodicesToImport { get; set; }

        public bool RemovePersonalData { get; set; } = true;

        //SETTINGS STEP

        //Auto Import Folders
        private bool _importAutoImportFolders = false;
        public bool ImportAutoImportFolders
        {
            get => (_importAutoImportFolders && AdvancedImport) || (ImportAllSettings && !AdvancedImport);
            set => SetProperty(ref _importAutoImportFolders, value);
        }
        public List<ImportPathHelper> AutoImportFolders { get; init; }

        //Banished paths
        private bool _importBanishedFiles = false;
        public bool ImportBanishedFiles
        {
            get => (_importBanishedFiles && AdvancedImport) || (ImportAllSettings && !AdvancedImport);
            set => SetProperty(ref _importBanishedFiles, value);
        }
        public List<ImportPathHelper> BanishedPaths { get; init; }

        //File type preferences
        private bool _importFileTypePrefs = false;
        public bool ImportFileTypePrefs
        {
            get => (_importFileTypePrefs && AdvancedImport) || (ImportAllSettings && !AdvancedImport);
            set => SetProperty(ref _importFileTypePrefs, value);
        }
        public List<ObservableKeyValuePair<string, bool>> FileTypePrefs { get; init; }

        //Tag-Folder links
        private bool _importFolderTagLinks = false;
        public bool ImportFolderTagLinks
        {
            get => (_importFolderTagLinks && AdvancedImport) || (ImportAllSettings && !AdvancedImport);
            set => SetProperty(ref _importFolderTagLinks, value);
        }
        public List<ImportFolderTagLinkHelper> FolderTagLinks { get; init; }

        public CollectionViewSource FolderTagLinksVS
        {
            get
            {
                CollectionViewSource temp = new()
                {
                    Source = FolderTagLinks,
                };
                temp.SortDescriptions.Add(new SortDescription("Folder", ListSortDirection.Ascending));
                return temp;
            }
        }

        #region Helper classes
        public class ImportPathHelper : ObservableObject
        {
            public ImportPathHelper(string path)
            {
                Path = path;
                ShouldImport = PathExits;
            }

            private bool _shouldImport;
            public bool ShouldImport
            {
                get => _shouldImport;
                set => SetProperty(ref _shouldImport, value);
            }

            public string Path { get; set; }

            public bool PathExits => !System.IO.Path.IsPathFullyQualified(Path) || System.IO.Path.Exists(Path);
        }

        public class ImportFolderTagLinkHelper : ImportPathHelper
        {
            public ImportFolderTagLinkHelper(string path, Tag t, IEnumerable<Tag> existingTags) : base(path)
            {
                Tag = t;
                _existingTags = existingTags;
            }
            private IEnumerable<Tag> _existingTags;
            public Tag Tag { get; }
            public bool TagExists => _existingTags.Contains(Tag);
        }

        public class ImportCodexHelper : ImportPathHelper
        {
            public ImportCodexHelper(Codex codex) : base(codex.Path)
            {
                Codex = codex;
            }
            public Codex Codex { get; }

            private RelayCommand<IList> _itemCheckedCommand;
            public RelayCommand<IList> ItemCheckedCommand => _itemCheckedCommand ??= new((items) =>
                items.Cast<ImportCodexHelper>()
                     .ToList()
                     .ForEach(helper => helper.ShouldImport = ShouldImport));


        }
        #endregion

        public override void Finish()
        {
            TargetCollection = MergeIntoCollection ?
                MainViewModel.CollectionVM.CurrentCollection :
                MainViewModel.CollectionVM.CreateAndLoadCollection(CollectionName);

            //add selected Tags to tmp collection
            if (AdvancedImport)
            {
                CollectionToImport.RootTags = CheckableTreeNode<Tag>.GetCheckedItems(TagsToImport).ToList();

                //Remove the tags that didn't make it from codices
                var RemovedTags = CollectionToImport.AllTags.Except(Utils.FlattenTree(CollectionToImport.RootTags)).ToList();
                foreach (Tag t in RemovedTags)
                {
                    CollectionToImport.AllTags.Remove(t);
                    foreach (var codex in CollectionToImport.AllCodices)
                    {
                        codex.Tags.Remove(t);
                    }
                }
            }
            else if (!ImportAllTags)
            {
                foreach (var tag in CollectionToImport.AllTags)
                {
                    foreach (var codex in CollectionToImport.AllCodices)
                    {
                        codex.Tags.Remove(tag);
                    }
                }
                CollectionToImport.RootTags.Clear();
                CollectionToImport.AllTags.Clear();
            }

            //add selected Codices to tmp collection
            if (RemovePersonalData)
            {
                CodicesToImport.ForEach(c => c.Codex.ClearPersonalData());
            }

            CollectionToImport.AllCodices.Clear();
            if (AdvancedImport)
            {
                CollectionToImport.AllCodices.AddRange(CodicesToImport.Where(x => x.ShouldImport).Select(x => x.Codex));
            }
            else if (ImportAllCodices)
            {
                CollectionToImport.AllCodices.AddRange(CodicesToImport.Select(x => x.Codex));
            }

            //Add selected Settings to tmp collection
            CollectionToImport.Info.AutoImportDirectories.Clear();
            if (ImportAutoImportFolders)
            {
                CollectionToImport.Info.AutoImportDirectories = new(AutoImportFolders.Where(x => x.ShouldImport).Select(x => x.Path));
            }
            CollectionToImport.Info.BanishedPaths.Clear();
            if (ImportBanishedFiles)
            {
                CollectionToImport.Info.BanishedPaths = new(BanishedPaths.Where(x => x.ShouldImport).Select(x => x.Path));
            }
            if (!ImportFileTypePrefs)
            {
                //for file types, import all or nothing because checking whether to import a checkbox becomes ridiculous
                CollectionToImport.Info.FiletypePreferences.Clear();
            }
            CollectionToImport.Info.FolderTagPairs.Clear();
            if (ImportFolderTagLinks)
            {
                CollectionToImport.Info.FolderTagPairs = new(FolderTagLinks.Where(linkHelper => linkHelper.ShouldImport && linkHelper.TagExists)
                                                                                   .Select(linkHelper => new FolderTagPair(linkHelper.Path, linkHelper.Tag)));
            }

            //If some tags are no longer present, they should be deleted from Codices
            TargetCollection.MergeWith(CollectionToImport);

            CloseAction.Invoke();
        }

        public void UpdateSteps()
        {
            //Checks which steps need to be included in wizard
            Steps.Clear();
            Steps.Add("Overview");
            if (AdvancedImport)
            {
                if (HasTags)
                {
                    Steps.Add("Tags");
                }
                if (HasCodices)
                {
                    Steps.Add("Codices");
                }
                if (HasSettings)
                {
                    Steps.Add("Settings");
                }
            }
            RaisePropertyChanged(nameof(Steps));
        }

        public void Cleanup() => MainViewModel.CollectionVM.DeleteCollection(CollectionToImport);
    }
}
