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
            CollectionToImport = new CodexCollection(_unzipLocation);
            CollectionToImport.Load(true);

            //Checks which steps need to be included in wizard
            if (CollectionToImport.AllCodices.Count > 0)
            {
                Steps.Add("Codices");
            }
            if (CollectionToImport.AllTags.Count > 0)
            {
                Steps.Add("Tags");
            }
            if (CollectionToImport.Info.ContainsSettings())
            {
                Steps.Add("Settings");
            }

            //Put codices in dictionary so they can be labeled true/false for import
            foreach (Codex codex in CollectionToImport.AllCodices)
            {
                CodexToImportDict.Add(codex, true);
            }

            //if files were included in compass file, set paths of codices to those files
            if (Directory.Exists(Path.Combine(_unzipLocation, "Files")))
            {
                foreach (Codex codex in CollectionToImport.AllCodices)
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
        public CodexCollection CollectionToImport { get; set; } = null; //null means new collection should be made
        public string CollectionName;

        // CODICES STEP
        public Dictionary<Codex, bool> CodexToImportDict { get; set; } = new();

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
            TargetCollection ??= MainViewModel.CollectionVM.CreateAndLoadCollection(CollectionName);

            CodexViewModel.MoveToCollection(TargetCollection, CodexToImportDict.Keys.Where(codex => CodexToImportDict[codex]).ToList());

            Utils.ClearTmpData(_unzipLocation);
            CloseAction.Invoke();
        }
    }
}
