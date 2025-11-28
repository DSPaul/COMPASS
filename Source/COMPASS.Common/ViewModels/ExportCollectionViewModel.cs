using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Selection;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

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

        protected override async Task Finish()
        {
            ApplyChoices();

            CloseAction?.Invoke();

            string? targetPath = await ChooseDestination();
            if (targetPath is null) return;

            await ExportToFile(targetPath);
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
                ContentSelectorVM.SelectAutoImportFolders = ExportAllSettings;
                ContentSelectorVM.SelectBanishedFiles = ExportAllSettings;
                ContentSelectorVM.SelectFileTypePrefs = ExportAllSettings;
                ContentSelectorVM.SelectFolderTagLinks = ExportAllSettings;
            }

            //Apply the selection
            ContentSelectorVM.ApplyAllSelections();
        }

        private async Task<string?> ChooseDestination()
        {
            var filesService = ServiceResolver.Resolve<IFilesService>();

            using var saveFile = await filesService.SaveFileAsync(new()
            {
                FileTypeChoices = [filesService.SatchelExtensionFilter],
                SuggestedFileName = CollectionToExport.Name,
                DefaultExtension = Constants.SatchelExtension
            });

            return saveFile?.Path.AbsolutePath;
        }

        public async Task ExportToFile(string targetPath)
        {
            var progressVM = ProgressViewModel.GetInstance();

            StorageStrategy targetFormat = StorageStrategy.Xml; //Could add new formats in the future
            var storageService = ServiceResolver.ResolveKeyed<ICodexCollectionStorageService>(targetFormat);
            
            try
            {
                
                //make sure to save first
                await storageService.AllocateNewCollection(ContentSelectorVM.CuratedCollection);
                storageService.Save(ContentSelectorVM.CuratedCollection);

                using var archive = ZipArchive.Create();

                //Change Codex Path to relative and add those files if the options is set
                var itemsWithOfflineSource = ContentSelectorVM.CuratedCollection.AllCodices
                    .Where(codex => codex.Sources.HasOfflineSource())
                    .ToList();
                string commonFolder = IOService.GetCommonFolder(itemsWithOfflineSource.Select(codex => codex.Sources.Path).ToList());
                foreach (Codex codex in itemsWithOfflineSource)
                {
                    string relativePath = codex.Sources.Path[commonFolder.Length..].TrimStart(Path.DirectorySeparatorChar);

                    //Add the file
                    if (IncludeFiles && File.Exists(codex.Sources.Path))
                    {
                        archive.AddEntry(Path.Combine("Files", relativePath), codex.Sources.Path);
                        //keep the relative path, will be used during import to link the included files
                        codex.Sources.Path = relativePath;
                    }

                    //absolute path is user specific, so counts as personal data 
                    if (ContentSelectorVM.RemovePersonalData)
                    {
                        codex.Sources.Path = relativePath;
                    }

                    //Add cover art
                    if (IncludeCoverArt && File.Exists(codex.CoverArtPath))
                    {
                        archive.AddEntry(Path.Combine("CoverArt", Path.GetFileName(codex.CoverArtPath)), codex.CoverArtPath);
                    }
                }

                //Save changes
                storageService.SaveCodices(ContentSelectorVM.CuratedCollection);

                //Now add files
                storageService.AddCollectionToArchive(archive, ContentSelectorVM.CuratedCollection);

                //Add the version so we can check compatibility when importing
                SatchelInfo info = new();
                archive.AddEntry(Constants.SatchelInfoFileName, new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(info)));

                //Progress reporting
                progressVM.Text = "Exporting Collection";
                progressVM.ShowCount = false;
                progressVM.ResetCounter();
                progressVM.TotalAmount = 1;
                //TODO find new way to track progress

                //Export
                await Task.Run(() => archive.SaveTo(targetPath, CompressionType.None));

                progressVM.IncrementCounter();
                Logger.Info($"Exported {CollectionToExport.Name} to {targetPath}");
            }
            catch (Exception ex)
            {
                Logger.Error("Export failed", ex);
                progressVM.Clear();
                CloseAction?.Invoke();
            }
            finally
            {
                storageService.DeleteCollection(ContentSelectorVM.CuratedCollection);
            }
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
