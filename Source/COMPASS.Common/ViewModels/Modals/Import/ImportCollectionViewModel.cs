using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Selection;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Modals.Import
{
    public class ImportCollectionViewModel : WizardViewModel, IDisposable
    {
        public override string WindowTitle { get; } = "Import Collection";

        private readonly WizardStepViewModel _overviewStep = new("Overview");

        private readonly CollectionHandle _collectionToImportHandle;
        
        public ImportCollectionViewModel(CodexCollectionVM collectionVmToImport)
        {
            AddValidation(nameof(MergeIntoCollection), ValidateTarget);
            AddValidation(nameof(CollectionName), ValidateTarget);
            AddValidation(nameof(TargetCollection), ValidateTarget);
            
            CollectionToImport = collectionVmToImport.Collection;
            TargetCollection = TabsViewModel.GetInstance().ActiveTab?.CollectionVM;
            CollectionVms = CollectionManager.CollectionVms.ToList(); 
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

        #region Properties
        
        public CollectionContentSelectorViewModel ContentSelectorVM { get; }

        public CodexCollection CollectionToImport { get; } //collection that was in the satchel

        public IList<CodexCollectionVM> CollectionVms { get; } 
        
        public CodexCollectionVM? TargetCollection { get; set => SetProperty(ref field, value); }
        
        /// <summary>
        /// Indicates that the tags should all be imported in a new, separate group
        /// </summary>
        public bool ImportTagsSeparately { get; set; } = false;

        //OVERVIEW STEP
        public bool MergeIntoCollection { get; set => SetProperty(ref field, value); }

        public string CollectionName { get; set => SetProperty(ref field, value); }

        public bool ImportAllTags { get; set; } = true;
        public bool ImportAllCodices { get; set; } = true;
        public bool ImportAllSettings { get; set; } = true;

        public bool AdvancedImport
        {
            get;
            set
            {
                SetProperty(ref field, value);
                UpdateSteps();
            }
        } = false;
        
        #endregion
        
        #region Wizard Viewmodel Overrides
        
        public override async Task Finish()
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
                        ImportTagsSeparately = true;
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

            CollectionHandle? targetCollectionHandle = null;
            try
            {
                targetCollectionHandle = MergeIntoCollection ? 
                    TargetCollection?.Load() : 
                    await CollectionManager.CreateAndLoadCollection(CollectionName);

                if (targetCollectionHandle == null)
                {
                    throw new LoadException(MergeIntoCollection ? TargetCollection!.Identifier : CollectionName);
                }

                //Save the changes to a permanent collection
                targetCollectionHandle.CollectionVM.Collection.MergeWith(ContentSelectorVM.CuratedCollection, ImportTagsSeparately);
                targetCollectionHandle.Save();
            }
            finally
            {
                targetCollectionHandle?.Dispose();
                CloseAction();
            }
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

        #endregion

        #region Methods

        private void ValidateTarget()
        {
            //Because a change in MergeIntoCollection can change validity, clear errors on the other props manually
            _overviewStep.ClearErrors(nameof(CollectionName));
            _overviewStep.ClearErrors(nameof(TargetCollection));
            ClearErrors(nameof(CollectionName));
            ClearErrors(nameof(TargetCollection));
            
            if (!MergeIntoCollection && !CollectionManager.IsLegalCollectionName(CollectionName))
            {
                //Error on step to block next
                _overviewStep.AddError(nameof(CollectionName), "Collection name is invalid");
                //Error on vm to show in UI
                AddError(nameof(CollectionName), "Collection name is invalid");
            }
            
            if (MergeIntoCollection && TargetCollection == null)
            {
                //Error on step to block next
                _overviewStep.AddError(nameof(TargetCollection), "Please select a target collection");
                //Error on vm to show in UI
                AddError(nameof(TargetCollection), "Please select a target collection");
            }
        }

        #endregion
        
        public void Dispose()
        {
            _collectionToImportHandle.Dispose();
            //temporary import collection has served its purpose, can be deleted
            _collectionToImportHandle.CollectionVM.DeleteCollection();
            _collectionToImportHandle.CollectionVM.Dispose();
        }
    }
}