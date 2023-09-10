using COMPASS.Models;
using COMPASS.Tools;
using Ionic.Zip;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace COMPASS.ViewModels.Import
{
    public class ImportCollectionViewModel : WizardViewModel
    {
        public ImportCollectionViewModel(MainViewModel mainViewModel, string toImport)
        {
            _mainVM = mainViewModel;

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

        private MainViewModel _mainVM { get; }
        private string _unzipLocation;

        public CodexCollection TargetCollection { get; set; } = null; //null means new collection should be made
        public CodexCollection RawCollectionToImport { get; set; } = null; //collection that was in the cmpss file
        public CodexCollection ReviewedCollectionToImport { get; set; } //collection that should actually be merged into targetCollection

        //Overview STEP
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
            //TODO

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
