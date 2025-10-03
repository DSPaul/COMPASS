using System;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Selection;

namespace COMPASS.Common.ViewModels.Import
{
    public class ImportCollectionViewModel : WizardViewModel, IDisposable
    {
        public override string WindowTitle { get; } = "Import Collection";

        private readonly WizardStepViewModel _overviewStep = new("Overview");

        private readonly CollectionHandle _collectionToImportHandle;
        
        public ImportCollectionViewModel(CodexCollectionVM collectionVmToImport)
        {
            CollectionToImport = collectionVmToImport.Collection;
            
            //Collection will have format '__<name><extension>'
            CollectionName = CollectionToImport.Name.Substring(2, CollectionToImport.Name.Length - 2 - Constants.SatchelExtension.Length);
            
            //temporarily register the collection to the manager so it can be loaded and read
            CollectionManager.RegisterCollection(collectionVmToImport);
            _collectionToImportHandle = collectionVmToImport.Load() ?? throw new LoadException(collectionVmToImport.Identifier);
            
            ContentSelectorVM = new(CollectionToImport);

            UpdateSteps();

            //if files were included in compass file, set paths of codices to those files
            var userFileService = ServiceResolver.Resolve<IUserFilesStorageService>();

            if (userFileService.HasUserFiles(CollectionToImport))
            {
                foreach (Codex codex in CollectionToImport.AllCodices.Where(c => c.Sources.HasOfflineSource()))
                {
                    userFileService.MoveCodexDataToCollection(codex, CollectionToImport);
                }
            }
        }

        public CollectionContentSelectorViewModel ContentSelectorVM { get; }

        public CodexCollection CollectionToImport { get; } //collection that was in the satchel

        /// <summary>
        /// Indicates that the tags should all be imported in a new, separate group
        /// </summary>
        public bool ImportTagsSeparatly { get; set; } = false;

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

        public bool IsCollectionNameLegal => CollectionManager.IsLegalCollectionName(CollectionName);

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
        protected override bool ShowNextButton() => base.ShowNextButton() &&
                                                    !(CurrentStep == _overviewStep && !MergeIntoCollection && !IsCollectionNameLegal);

        protected override bool ShowFinishButton() => base.ShowFinishButton() &&
                                                      !(CurrentStep == _overviewStep && !MergeIntoCollection && !IsCollectionNameLegal);

        protected override async Task Finish()
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

            //If we have tags and are doing an advanced import, but the tags step is missing, ask if we should still import them
            if (AdvancedImport // if we are doing an advanced import
                && !Steps.Contains(CollectionContentSelectorViewModel.TagsStep) // and the tags step is missing
                && ContentSelectorVM.HasTags // but there are tags included in the import
                && ContentSelectorVM.SelectableCodices // and the tags are present on the chosen codices
                    .Where(x => x.Selected)
                    .SelectMany(sc => sc.Codex.Tags)
                    .Any())
            {
                string message = $"Some of the items that you are about to import have Tags, \n " +
                                 $"would you like to import these as well?\n " +
                                 $"They will be put in a new tag group called {CollectionName}.";

                Notification notification = new("Tags found", message, Severity.Info,
                    NotificationAction.Cancel | NotificationAction.Decline | NotificationAction.Confirm);
                await ServiceResolver.Resolve<INotificationService>().ShowDialog(notification);

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

            CollectionHandle targetCollectionHandle;
            if (MergeIntoCollection)
            {
                targetCollectionHandle = ActiveCollection.Load() ?? throw new LoadException(ActiveCollection.Name);
            }
            else
            {
                var newHandle = await CollectionManager.CreateAndLoadCollection(CollectionName);
                if (newHandle == null)
                {
                    //TODO
                    throw new Exception($"Collection {CollectionName} could not be created");
                }
                else
                {
                    targetCollectionHandle = newHandle;
                }
            }
            
            //Save the changes to a permanent collection
            CodexCollection targetCollection = MergeIntoCollection
                ? ActiveCollection
                : targetCollectionHandle.CollectionVM.Collection;

            targetCollection.MergeWith(ContentSelectorVM.CuratedCollection, ImportTagsSeparatly);
            targetCollection.Save();
            targetCollectionHandle.Dispose();
            CloseAction();
        }

        private void UpdateSteps()
        {
            //Checks which steps need to be included in wizard
            Steps.Clear();
            Steps.Add(_overviewStep);
            if (AdvancedImport)
            {
                ContentSelectorVM.UpdateSteps();
                Steps.AddRange(ContentSelectorVM.Steps);
            }
        }

        public void Dispose()
        {
            //temporary import collection has served its purpose
            CollectionManager.DeleteCollection(_collectionToImportHandle);
        }
    }
}