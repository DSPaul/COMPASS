using COMPASS.Models;
using COMPASS.Tools;
using Ionic.Zip;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace COMPASS.ViewModels.Import
{
    public class ImportCollectionViewModel : WizardViewModel
    {
        public ImportCollectionViewModel(string toImport)
        {

            //unzip and load collection to import
            _unzipLocation = UnZipCollection(toImport);
            RawCollectionToImport = new CodexCollection(Path.GetFileName(_unzipLocation));
            RawCollectionToImport.Load(true);
            ReviewedCollectionToImport = new(RawCollectionToImport.DirectoryName + "__Reviewed");

            //Checks which steps need to be included in wizard
            HasCodices = RawCollectionToImport.AllCodices.Any();
            HasTags = RawCollectionToImport.AllTags.Any();
            HasSettings = RawCollectionToImport.Info.ContainsSettings();
            UpdateSteps();

            //Put Tags in Checkable Wrapper
            TagsToImport = RawCollectionToImport.RootTags.Select(t => new CheckableTreeNode<Tag>(t)).ToList();

            //Put codices in dictionary so they can be labeled true/false for import
            foreach (Codex codex in RawCollectionToImport.AllCodices)
            {
                CodexToImportDict.Add(new(codex, true));
            }

            //prep settings data for selection
            AutoImportFolders = RawCollectionToImport.Info.AutoImportDirectories.Select(folder => new ImportPathHelper(folder)).ToList();
            BanishedPaths = RawCollectionToImport.Info.BanishedPaths.Select(path => new ImportPathHelper(path)).ToList();
            FileTypePrefs = RawCollectionToImport.Info.FiletypePreferences
                                                            .Select(x => new ObservableKeyValuePair<string, bool>(x))
                                                            .OrderByDescending(x => x.Value)
                                                            .ToList();
            FolderTagLinks = RawCollectionToImport.Info.FolderTagPairs
                .Select(link => new ImportFolderTagLinkHelper(link.Folder, link.Tag, Utils.FlattenTree(CheckableTreeNode<Tag>.GetCheckedItems(TagsToImport))))
                .ToList();

            //if files were included in compass file, set paths of codices to those files
            if (Directory.Exists(Path.Combine(_unzipLocation, "Files")))
            {
                foreach (Codex codex in RawCollectionToImport.AllCodices)
                {
                    string includedFilePath = Path.Combine(_unzipLocation, "Files", Path.GetFileName(codex.Path));
                    if (File.Exists(includedFilePath))
                    {
                        codex.Path = includedFilePath;
                    }
                }
            }
        }

        private string _unzipLocation;

        public CodexCollection TargetCollection { get; set; } = null; //null means new collection should be made
        public CodexCollection RawCollectionToImport { get; set; } = null; //collection that was in the cmpss file
        public CodexCollection ReviewedCollectionToImport { get; set; } //collection that should actually be merged into targetCollection

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
        public List<ObservableKeyValuePair<Codex, bool>> CodexToImportDict { get; set; } = new();

        //SETTINGS STEP

        //Auto Import Folders
        private bool _importAutoImportFolders = false;
        public bool ImportAutoImportFolders
        {
            get => _importAutoImportFolders;
            set => SetProperty(ref _importAutoImportFolders, value);
        }
        public List<ImportPathHelper> AutoImportFolders { get; init; }

        //Banished paths
        private bool _importBanishedFiles = false;
        public bool ImportBanishedFiles
        {
            get => _importBanishedFiles;
            set => SetProperty(ref _importBanishedFiles, value);
        }
        public List<ImportPathHelper> BanishedPaths { get; init; }

        //File type preferences
        private bool _importFileTypePrefs = false;
        public bool ImportFileTypePrefs
        {
            get => _importFileTypePrefs;
            set => SetProperty(ref _importFileTypePrefs, value);
        }
        public List<ObservableKeyValuePair<string, bool>> FileTypePrefs { get; init; }

        //Tag-Folder links
        private bool _importFolderTagLinks = false;
        public bool ImportFolderTagLinks
        {
            get => _importFolderTagLinks;
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

        //helper class
        public class ImportPathHelper
        {
            public ImportPathHelper(string path)
            {
                Path = path;
                ShouldImport = PathExits;
            }
            public bool ShouldImport { get; set; }

            public string Path { get; set; }

            public bool PathExits => !System.IO.Path.IsPathFullyQualified(Path) || System.IO.Path.Exists(Path);
        }

        public class ImportFolderTagLinkHelper : ImportPathHelper
        {
            public ImportFolderTagLinkHelper(string path) : base(path) { }
            public ImportFolderTagLinkHelper(string path, Tag t, IEnumerable<Tag> existingTags) : this(path)
            {
                Tag = t;
                _existingTags = existingTags;
            }
            private IEnumerable<Tag> _existingTags;
            public Tag Tag { get; }
            public bool TagExists => _existingTags.Contains(Tag);
        }

        private string UnZipCollection(string path)
        {
            string fileName = Path.GetFileName(path);
            string tmpCollectionPath = Path.Combine(CodexCollection.CollectionsPath, $"__{fileName}");
            //make sure any previous temp data is gone
            Utils.ClearTmpData(tmpCollectionPath);
            //unzip the file to tmp folder
            ZipFile zip = ZipFile.Read(path);
            zip.ExtractAll(tmpCollectionPath);
            return tmpCollectionPath;
        }

        public override void Finish()
        {
            TargetCollection = MergeIntoCollection ?
                MainViewModel.CollectionVM.CurrentCollection :
                MainViewModel.CollectionVM.CreateAndLoadCollection(CollectionName);

            //add selected Tags to tmp collection
            if (AdvancedImport || ImportAllTags)
            {
                ReviewedCollectionToImport.RootTags = CheckableTreeNode<Tag>.GetCheckedItems(TagsToImport).ToList();
            }

            //add selected Codices to tmp collection
            if (AdvancedImport || ImportAllCodices)
            {
                ReviewedCollectionToImport.AllCodices
                .AddRange(CodexToImportDict
                    .Where(KVPair => KVPair.Value)
                    .Select(KVPair => KVPair.Key));
            }

            //Add selected Settings to tmp collection
            if (ImportAutoImportFolders)
            {
                ReviewedCollectionToImport.Info.AutoImportDirectories = new(AutoImportFolders.Where(x => x.ShouldImport).Select(x => x.Path));
            }
            if (ImportBanishedFiles)
            {
                ReviewedCollectionToImport.Info.BanishedPaths = new(BanishedPaths.Where(x => x.ShouldImport).Select(x => x.Path));
            }
            if (ImportFileTypePrefs)
            {
                //for file types, import all or nothing because checking whether to import a checkbox becomes ridiculous
                ReviewedCollectionToImport.Info.FiletypePreferences = new(RawCollectionToImport.Info.FiletypePreferences);
            }
            if (ImportFolderTagLinks)
            {
                ReviewedCollectionToImport.Info.FolderTagPairs = new(FolderTagLinks.Where(linkHelper => linkHelper.ShouldImport && linkHelper.TagExists)
                                                                                   .Select(linkHelper => new FolderTagPair(linkHelper.Path, linkHelper.Tag)));
            }

            TargetCollection.MergeWith(ReviewedCollectionToImport);

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

        public void Cleanup()
        {
            MainViewModel.CollectionVM.DeleteCollection(RawCollectionToImport);
            MainViewModel.CollectionVM.DeleteCollection(ReviewedCollectionToImport);
        }
    }
}
