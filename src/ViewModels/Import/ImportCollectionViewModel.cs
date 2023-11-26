using COMPASS.Models;
using COMPASS.Tools;
using System.IO;

namespace COMPASS.ViewModels.Import
{
    public class ImportCollectionViewModel : WizardViewModel
    {
        public ImportCollectionViewModel(CodexCollection collectionToImport)
        {
            CollectionToImport = collectionToImport;
            CollectionName = CollectionToImport.DirectoryName.Substring(2, CollectionToImport.DirectoryName.Length - 2 - Constants.COMPASSFileExtension.Length);

            ContentSelectorVM = new(collectionToImport);
            ContentSelectorVM.CuratedCollection = collectionToImport; //The import collection is tmp anyway so result can be saved on top of it

            UpdateSteps();

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

        public CollectionContentSelectorViewModel ContentSelectorVM { get; set; }

        public CodexCollection CollectionToImport { get; set; } = null; //collection that was in the cmpss file

        //OVERVIEW STEP
        public bool MergeIntoCollection { get; set; } = false;

        private string _collectionName = "Unnamed Collection";
        public string CollectionName
        {
            get => _collectionName;
            set
            {
                SetProperty(ref _collectionName, value);
                RaisePropertyChanged(nameof(IsCollectionNameLegal));
            }
        }
        public bool IsCollectionNameLegal => CollectionViewModel.IsLegalCollectionName(CollectionName);

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

        //Don't show on overview tab if new collection is chosen with illegal name
        public override bool ShowNextButton() => base.ShowNextButton() &&
            !(CurrentStep == "Overview" && !MergeIntoCollection && !IsCollectionNameLegal);
        public override bool ShowFinishButton() => base.ShowFinishButton() &&
            !(CurrentStep == "Overview" && !MergeIntoCollection && !IsCollectionNameLegal);

        public override void ApplyAll()
        {
            //if we do a quick import, set all the things in the contentSelector have the right value
            if (!AdvancedImport)
            {
                //Set it on tags
                foreach (var selectableTag in ContentSelectorVM.SelectableTags)
                {
                    selectableTag.IsChecked = ImportAllTags;
                }

                //Set it on codices
                foreach (var selectableCodex in ContentSelectorVM.SelectableCodices)
                {
                    selectableCodex.Selected = ImportAllCodices;
                }

                //Set it on all the settings
                ContentSelectorVM.SelectAutoImportFolders = ImportAllSettings;
                ContentSelectorVM.SelectBanishedFiles = ImportAllSettings;
                ContentSelectorVM.SelectFileTypePrefs = ImportAllSettings;
                ContentSelectorVM.SelectFolderTagLinks = ImportAllSettings;
            }

            //Apply the selection
            ContentSelectorVM.ApplyAll();

            //Save the changes to a permanent collection
            var targetCollection = MergeIntoCollection ?
                MainViewModel.CollectionVM.CurrentCollection :
                MainViewModel.CollectionVM.CreateAndLoadCollection(CollectionName);

            targetCollection.MergeWith(ContentSelectorVM.CuratedCollection);

            CloseAction.Invoke();
        }

        public void UpdateSteps()
        {
            //Checks which steps need to be included in wizard
            Steps.Clear();
            Steps.Add("Overview");
            if (AdvancedImport)
            {
                ContentSelectorVM.UpdateSteps();
                Steps.AddRange(ContentSelectorVM.Steps);
            }
            RaisePropertyChanged(nameof(Steps));
        }

        public void Cleanup() => MainViewModel.CollectionVM.DeleteCollection(CollectionToImport);
    }
}
