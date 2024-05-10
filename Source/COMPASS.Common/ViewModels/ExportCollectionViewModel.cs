using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels
{
    public class ExportCollectionViewModel : WizardViewModel
    {
        public ExportCollectionViewModel() : this(MainViewModel.CollectionVM.CurrentCollection) { }
        public ExportCollectionViewModel(CodexCollection collectionToExport)
        {
            CollectionToExport = collectionToExport;

            ContentSelectorVM = new(collectionToExport)
            {
                CuratedCollection = new CodexCollection("__export__tmp")
            };

            UpdateSteps();
        }

        public CollectionContentSelectorViewModel ContentSelectorVM { get; set; }

        public CodexCollection CollectionToExport { get; set; }

        //OVERVIEW STEP
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

        private RelayCommand? _applyActiveFiltesCommand;
        public RelayCommand ApplyActiveFiltersCommand => _applyActiveFiltesCommand ??=
            new(ApplyActiveFilters, () => MainViewModel.CollectionVM.FilterVM.HasActiveFilters);
        private void ApplyActiveFilters()
        {
            foreach (var selectableCodex in ContentSelectorVM.SelectableCodices)
            {
                selectableCodex.Selected = MainViewModel.CollectionVM.FilterVM.FilteredCodices!.Contains(selectableCodex.Codex);
            }
            ContentSelectorVM.RaiseSelectedCodicesCountChanged();
        }

        public bool IncludeFiles { get; set; }
        public bool IncludeCoverArt { get; set; }

        public override async Task Finish()
        {
            await ApplyChoices();

            CloseAction?.Invoke();

            string? targetPath = ChooseDestination();
            if (targetPath is null) return;

            await ExportToFile(targetPath);
        }

        public async Task ApplyChoices()
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
            await ContentSelectorVM.Finish();
        }

        private string? ChooseDestination()
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = Constants.SatchelExtensionFilter,
                FileName = CollectionToExport.DirectoryName,
                DefaultExt = Constants.SatchelExtension
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                return saveFileDialog.FileName;
            }
            return null;
        }

        public async Task ExportToFile(string targetPath)
        {
            var progressVM = ProgressViewModel.GetInstance();

            try
            {
                //make sure to save first
                ContentSelectorVM.CuratedCollection.InitAsNew();
                ContentSelectorVM.CuratedCollection.Save();

                using var archive = ZipArchive.Create();

                //Change Codex Path to relative and add those files if the options is set
                var itemsWithOfflineSource = ContentSelectorVM.CuratedCollection.AllCodices
                    .Where(codex => codex.HasOfflineSource())
                    .ToList();
                string commonFolder = IOService.GetCommonFolder(itemsWithOfflineSource.Select(codex => codex.Path).ToList());
                foreach (Codex codex in itemsWithOfflineSource)
                {
                    string relativePath = codex.Path[commonFolder.Length..].TrimStart(Path.DirectorySeparatorChar);

                    //Add the file
                    if (IncludeFiles && File.Exists(codex.Path))
                    {
                        archive.AddEntry(Path.Combine("Files", relativePath), codex.Path);
                        //keep the relative path, will be used during import to link the included files
                        codex.Path = relativePath;
                    }

                    //absolute path is user specific, so counts as personal data 
                    if (ContentSelectorVM.RemovePersonalData)
                    {
                        codex.Path = relativePath;
                    }

                    //Add cover art
                    if (IncludeCoverArt && File.Exists(codex.CoverArt))
                    {
                        archive.AddEntry(Path.Combine("CoverArt", Path.GetFileName(codex.CoverArt)), codex.CoverArt);
                    }
                }

                //Save changes
                ContentSelectorVM.CuratedCollection.SaveCodices();

                //Now add xml files
                archive.AddAllFromDirectory(ContentSelectorVM.CuratedCollection.FullDataPath);

                //Add version so we can check compatibility when importing
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
                Logger.Info($"Exported {CollectionToExport.DirectoryName} to {targetPath}");
            }
            catch (Exception ex)
            {
                Logger.Error("Export failed", ex);
                progressVM.Clear();
                CloseAction?.Invoke();
            }
            finally
            {
                Directory.Delete(ContentSelectorVM.CuratedCollection.FullDataPath, true);
            }
        }

        public void UpdateSteps()
        {
            //Checks which steps need to be included in wizard
            Steps.Clear();
            Steps.Add("Overview");
            if (AdvancedExport)
            {
                ContentSelectorVM.UpdateSteps();
                Steps.AddRange(ContentSelectorVM.Steps);
            }
        }
    }
}
