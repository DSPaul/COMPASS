using COMPASS.Models;
using COMPASS.Tools;
using Ionic.Zip;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class ExportCollectionViewModel : WizardViewModel
    {
        public ExportCollectionViewModel(CodexCollection collectionToExport)
        {
            CollectionToExport = collectionToExport;

            ContentSelectorVM = new(collectionToExport);
            ContentSelectorVM.CuratedCollection = new CodexCollection("__export__tmp");

            UpdateSteps();
        }

        public CollectionContentSelectorViewModel ContentSelectorVM { get; set; }

        public CodexCollection CollectionToExport { get; set; } = null;

        //OVERVIEW STEP

        public bool ExportAllTags { get; set; } = true;
        public bool ExportAllCodices { get; set; } = true;
        public bool ExportAllSettings { get; set; } = true;

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

        public bool IncludeFiles { get; set; }

        public async override void ApplyAll()
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
            ContentSelectorVM.ApplyAll();

            CloseAction.Invoke();

            await ExportToFile();

        }

        public async Task ExportToFile()
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = $"COMPASS File (*{Constants.COMPASSFileExtension})|*{Constants.COMPASSFileExtension}",
                FileName = CollectionToExport.DirectoryName,
                DefaultExt = Constants.COMPASSFileExtension
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                //make sure to save first
                ContentSelectorVM.CuratedCollection.CreateDirectories();
                ContentSelectorVM.CuratedCollection.Save();

                string targetPath = saveFileDialog.FileName;
                using ZipFile zip = new();
                zip.AddDirectory(ContentSelectorVM.CuratedCollection.FullDataPath);

                //Change Codex Path to relative and add those files if the options is set
                var itemsWithOfflineSource = ContentSelectorVM.CuratedCollection.AllCodices.Where(codex => codex.HasOfflineSource());
                string commonFolder = Utils.GetCommonFolder(itemsWithOfflineSource.Select(codex => codex.Path).ToList());
                foreach (Codex codex in itemsWithOfflineSource)
                {
                    string relativePath = codex.Path[commonFolder.Length..].TrimStart(Path.DirectorySeparatorChar);
                    if (IncludeFiles && File.Exists(codex.Path))
                    {
                        int index_start_filename = relativePath.Length - Path.GetFileName(codex.Path).Length;
                        zip.AddFile(codex.Path, Path.Combine("Files", relativePath[0..index_start_filename]));
                    }
                    //strip longest common path so relative paths stay, given that full paths will break anyway
                    codex.Path = relativePath;
                }

                ContentSelectorVM.CuratedCollection.SaveCodices();
                zip.UpdateFile(ContentSelectorVM.CuratedCollection.CodicesDataFilePath, "");

                //Progress reporting
                var ProgressVM = ProgressViewModel.GetInstance();
                ProgressVM.Text = "Exporting Collection";
                ProgressVM.ShowCount = false;
                ProgressVM.ResetCounter();
                zip.SaveProgress += (object _, SaveProgressEventArgs args) =>
                {
                    ProgressVM.TotalAmount = Math.Max(ProgressVM.TotalAmount, args.EntriesTotal);
                    if (args.EventType == ZipProgressEventType.Saving_AfterWriteEntry)
                    {
                        ProgressVM.IncrementCounter();
                    }
                };

                //Export
                await Task.Run(() =>
                {
                    zip.Save(targetPath);
                    Directory.Delete(ContentSelectorVM.CuratedCollection.FullDataPath, true);
                    ProgressVM.ShowCount = false;
                });
                Logger.Info($"Exported {CollectionToExport.DirectoryName} to {targetPath}");
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
            RaisePropertyChanged(nameof(Steps));
        }
    }
}
