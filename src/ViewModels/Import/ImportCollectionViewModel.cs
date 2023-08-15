using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace COMPASS.ViewModels.Import
{
    public class ImportCollectionViewModel : ObservableObject
    {
        public ImportCollectionViewModel(MainViewModel mainViewModel, string toImport)
        {
            _mainVM = mainViewModel;

            _unzipLocation = UnZipCollection(toImport);
            CollectionToImport = new CodexCollection(_unzipLocation);
            CollectionToImport.Load(true);

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

        //Steps in wizard:
        // - Choose which codices to import
        // - Choose which Tags to import
        // - Choose which settings to import
        // - Finish
        private int _wizardStepsCounter = 0;
        public int WizardStepsCounter
        {
            get => _wizardStepsCounter;
            set
            {
                if (value <= 0) value = 0;
                else if (value >= 3) Finish();
                WizardStepsCounter = value;
            }
        }

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

        private ActionCommand _nextStepCommand;
        public ActionCommand NextStepCommand => _nextStepCommand ??= new(() => WizardStepsCounter++);

        private ActionCommand _prevStepCommand;
        public ActionCommand PrevStepCommand => _prevStepCommand ??= new(() => WizardStepsCounter--);

        public void Finish()
        {
            if (TargetCollection is null)
            {
                MainViewModel.CollectionVM.CreateAndLoadCollection(CollectionName);
            }

            CodexViewModel.MoveToCollection(TargetCollection, CodexToImportDict.Keys.Where(codex => CodexToImportDict[codex]).ToList());

            Utils.ClearTmpData(_unzipLocation);
            CloseAction.Invoke();
        }

        public Action CloseAction;
    }
}
