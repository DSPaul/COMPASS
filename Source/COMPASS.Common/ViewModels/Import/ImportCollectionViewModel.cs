using Autofac;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Tools;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Operations;

namespace COMPASS.Common.ViewModels.Import
{
    public class ImportCollectionViewModel : WizardViewModel
    {
        public ImportCollectionViewModel(CodexCollection collectionToImport)
        {
            CollectionToImport = collectionToImport;
            CollectionName = CollectionToImport.Name.Substring(2, CollectionToImport.Name.Length - 2 - Constants.SatchelExtension.Length);

            ContentSelectorVM = new(collectionToImport)
            {
                CuratedCollection = collectionToImport //The import collection is tmp anyway so result can be saved on top of it
            };

            UpdateSteps();

            //if files were included in compass file, set paths of codices to those files
            var userFileService = App.Container.Resolve<IUserFilesStorageService>();

            if (userFileService.HasUserFiles(CollectionToImport))
            {
                foreach (Codex codex in CollectionToImport.AllCodices.Where(c => c.Sources.HasOfflineSource()))
                {
                    userFileService.MoveCodexDataToCollection(codex, CollectionToImport);
                }
            }
        }

        public CollectionContentSelectorViewModel ContentSelectorVM { get; set; }

        public CodexCollection CollectionToImport { get; set; } //collection that was in the satchel

        /// <summary>
        /// Indicates that the tags should all be imported in a new, separate group
        /// </summary>
        public bool ImportTagsSeparatly { get; set; } = false;

        private bool _deleteSatchelOnWizardClosing = true; //Delete the satchel if the wizard closes for any reason

        //OVERVIEW STEP
        private bool _mergeIntoCollection = false;
        public bool MergeIntoCollection
        {
            get => _mergeIntoCollection;
            set
            {
                SetProperty(ref _mergeIntoCollection, value);
                RefreshNavigationBtns();
            }
        }

        private string _collectionName = "Unnamed Collection";
        public string CollectionName
        {
            get => _collectionName;
            set
            {
                SetProperty(ref _collectionName, value);
                OnPropertyChanged(nameof(IsCollectionNameLegal));
                RefreshNavigationBtns();
            }
        }
        public bool IsCollectionNameLegal => CodexCollectionOperations.IsLegalCollectionName(CollectionName, MainViewModel.CollectionVM.AllCodexCollections);

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

        public override async Task Finish()
        {
            _deleteSatchelOnWizardClosing = false; //need to keep files around to merge files and cover art
            CloseAction?.Invoke();

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

            //If we have tags and are doing an advanced import, but the tags step is missing, ask if we should still import them
            if (AdvancedImport                                                   // if we are doing an advanced import
                && !Steps.Contains(CollectionContentSelectorViewModel.TagsStep)  // and the tags step is missing
                && ContentSelectorVM.HasTags                                     // but there are tags included in the import
                && ContentSelectorVM.SelectableCodices                           // and the tags are present on the chosen codices
                    .Where(x => x.Selected)
                    .SelectMany(sc => sc.Codex.Tags)
                    .Any())
            {
                string message = $"Some of the items that you are about to import have Tags, \n " +
                    $"would you like to import these as well?\n " +
                    $"They will be put in a new tag group called {CollectionName}.";

                Notification notification = new("Tags found", message, Severity.Info, NotificationAction.Cancel | NotificationAction.Decline | NotificationAction.Confirm);
                await App.Container.Resolve<INotificationService>().ShowDialog(notification);

                switch (notification.Result)
                {
                    case NotificationAction.Cancel:
                        return;
                    case NotificationAction.Confirm:
                        ImportTagsSeparatly = true;
                        ContentSelectorVM.OnlyTagsOnCodices = true;
                        break;
                    case NotificationAction.Decline:
                        //make sure to deselect all tags
                        ContentSelectorVM.TagsSelectorVM.SelectedTagCollection!.TagsRoot.IsChecked = false;
                        break;
                }
            }

            //Apply the selection
            ContentSelectorVM.ApplyAllSelections();

            //Save the changes to a permanent collection
            var targetCollection = MergeIntoCollection ?
                MainViewModel.CollectionVM.CurrentCollection :
                await MainViewModel.CollectionVM.CreateAndLoadCollection(CollectionName);

            //if create and load fails
            if (targetCollection is null)
            {
                //TODO IDK, show an error of some kind
                return;
            }

            await targetCollection.MergeWith(ContentSelectorVM.CuratedCollection, ImportTagsSeparatly);

            Cleanup();
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
        }

        public void Cleanup() => MainViewModel.CollectionVM.DeleteCollection(CollectionToImport);
        public void OnWizardClosing()
        {
            if (_deleteSatchelOnWizardClosing)
            {
                Cleanup();
            }
        }
    }
}
