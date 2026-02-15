using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Selection;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels
{
    public class ExportCollectionViewModel : WizardViewModel
    {
        public ExportCollectionViewModel() : this(TabsViewModel.GetInstance().ActiveTab!.CollectionVM.Collection) { }
        public ExportCollectionViewModel(CodexCollection collectionToExport)
        {
            CollectionToExport = collectionToExport;
            ContentSelectorVM = new(collectionToExport);
            UpdateSteps();
        }

        public CollectionContentSelectorViewModel ContentSelectorVM { get; set; }

        public CodexCollection CollectionToExport { get; }

        public override string WindowTitle { get; } = "Export Collection";
        
        //OVERVIEW STEP
        private readonly WizardStepViewModel _overviewStep = new("Overview");
        public bool ExportAllTags { get; set; } = true;
        public bool ExportAllCodices { get; set; } = true;
        public bool ExportAllSettings { get; set; } = false;

        private bool _advancedExport = false;
        public bool AdvancedExport
        {
            get => _advancedExport;
            set
            {
                SetProperty(ref _advancedExport, value);
                UpdateSteps();
            }
        }

        private RelayCommand? _applyActiveFiltersCommand;
        public RelayCommand ApplyActiveFiltersCommand => _applyActiveFiltersCommand ??=
            new(ApplyActiveFilters, () => TabsViewModel.GetInstance().ActiveTab?.FiltersVM.HasActiveFilters ?? false);
        private void ApplyActiveFilters()
        {
            foreach (var selectableCodex in ContentSelectorVM.SelectableCodices)
            {
                selectableCodex.Selected = TabsViewModel.GetInstance().ActiveTab!.FiltersVM.FilteredCodices
                                                        .Select(codexVm => codexVm.GetModel())
                                                        .Contains(selectableCodex.Codex);
            }
            ContentSelectorVM.RaiseSelectedCodicesCountChanged();
        }

        public bool IncludeFiles { get; set; }
        public bool IncludeCoverArt { get; set; }

        public override async Task Finish()
        {
            ApplyChoices();

            CloseAction?.Invoke();

            IStorageFile? targetFile = await ChooseDestination();
            if (targetFile is null) return;

            await ExportToFile(targetFile);
        }

        public void ApplyChoices()
        {
            //if we do a quick import, set all the things in the contentSelector have the right value
            if (!AdvancedExport)
            {
                //Set it on tags
                foreach (var selectableTag in ContentSelectorVM.SelectableTags)
                {
                    selectableTag.IsChecked = ExportAllTags;
                }

                //Set it on codices
                foreach (var selectableCodex in ContentSelectorVM.SelectableCodices)
                {
                    selectableCodex.Selected = ExportAllCodices;
                }

                //Set it on all the settings
                ContentSelectorVM.AutoImportFoldersSelector.SelectAll = ExportAllSettings;
                ContentSelectorVM.BanishedPathsSelector.SelectAll = ExportAllSettings;
                ContentSelectorVM.FileTypePrefsSelector.SelectAll = ExportAllSettings;
            }

            //Apply the selection
            ContentSelectorVM.ApplyAllSelections();
        }

        private async Task<IStorageFile?> ChooseDestination()
        {
            var filesService = ServiceResolver.Resolve<IFilesService>();

            return await filesService.SaveFileAsync(new()
            {
                FileTypeChoices = [filesService.SatchelExtensionFilter],
                SuggestedFileName = CollectionToExport.Name,
                DefaultExtension = Constants.SatchelExtension
            });
        }

        public async Task ExportToFile(IStorageFile targetFile)
        {
            var exportService = ServiceResolver.Resolve<IImportExportService>();
            await exportService.ExportCollection(ContentSelectorVM.CuratedCollection, targetFile, IncludeFiles, IncludeCoverArt);           
        }

        public void UpdateSteps()
        {
            //Checks which steps need to be included in wizard
            Steps.Clear();
            Steps.Add(_overviewStep);
            if (AdvancedExport)
            {
                ContentSelectorVM.UpdateSteps();
                Steps.AddRange(ContentSelectorVM.Steps);
            }
        }
    }
}
