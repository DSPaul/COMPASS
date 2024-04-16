using CommunityToolkit.Mvvm.Input;
using COMPASS.Models;
using COMPASS.Services;
using COMPASS.Tools;
using Ionic.Zip;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
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

                using ZipFile zip = new();
                zip.AddDirectory(ContentSelectorVM.CuratedCollection.FullDataPath);

                //Change Codex Path to relative and add those files if the options is set
                var itemsWithOfflineSource = ContentSelectorVM.CuratedCollection.AllCodices
                    .Where(codex => codex.HasOfflineSource())
                    .ToList();
                string commonFolder = IOService.GetCommonFolder(itemsWithOfflineSource.Select(codex => codex.Path).ToList());
                foreach (Codex codex in itemsWithOfflineSource)
                {
                    string relativePath = codex.Path[commonFolder.Length..].TrimStart(Path.DirectorySeparatorChar);
                    if (IncludeFiles && File.Exists(codex.Path))
                    {
                        int indexStartFilename = relativePath.Length - Path.GetFileName(codex.Path)!.Length;
                        zip.AddFile(codex.Path, Path.Combine("Files", relativePath[0..indexStartFilename]));
                        //keep the relative path, will be used during import to link the included files
                        codex.Path = relativePath;
                    }

                    //absolute path is user specific, so counts as personal data 
                    if (ContentSelectorVM.RemovePersonalData)
                    {
                        codex.Path = relativePath;
                    }
                }

                ContentSelectorVM.CuratedCollection.SaveCodices();
                zip.UpdateFile(ContentSelectorVM.CuratedCollection.CodicesDataFilePath, "");

                //Progress reporting
                progressVM.Text = "Exporting Collection";
                progressVM.ShowCount = false;
                progressVM.ResetCounter();
                zip.SaveProgress += (_, args) =>
                {
                    progressVM.TotalAmount = Math.Max(progressVM.TotalAmount, args.EntriesTotal);
                    if (args.EventType == ZipProgressEventType.Saving_AfterWriteEntry)
                    {
                        progressVM.IncrementCounter();
                    }
                };

                //Add version so we can check compatibility when importing
                SatchelInfo info = new();
                zip.AddEntry(Constants.SatchelInfoFileName, JsonSerializer.Serialize(info));

                //Export
                await Task.Run(() =>
                {
                    zip.Save(targetPath);
                    Directory.Delete(ContentSelectorVM.CuratedCollection.FullDataPath, true);
                });
                Logger.Info($"Exported {CollectionToExport.DirectoryName} to {targetPath}");
            }
            catch (Exception ex)
            {
                Logger.Error("Export failed", ex);
                progressVM.Clear();
                CloseAction?.Invoke();
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
            OnPropertyChanged(nameof(Steps));
        }
    }
}
