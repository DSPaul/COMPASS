using COMPASS.Models;
using COMPASS.Models.Enums;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Import
{
    public static class ImportViewModel
    {
        public static async Task Import(ImportSource source) => await Import(source, MainViewModel.CollectionVM.CurrentCollection);
        public static async Task Import(ImportSource source, CodexCollection targetCollection)
        {
            List<string> pathsToImport;
            switch (source)
            {
                case ImportSource.File:
                    pathsToImport = ChooseFiles();
                    await ImportFilesAsync(pathsToImport, targetCollection);
                    break;
                case ImportSource.Folder:
                    ImportFolderViewModel importFolderVM = new(targetCollection, manuallyTriggered: true);
                    await importFolderVM.Import();
                    break;
                case ImportSource.Manual:
                    ImportManual();
                    break;
                case ImportSource.GmBinder:
                case ImportSource.Homebrewery:
                case ImportSource.GoogleDrive:
                case ImportSource.GenericURL:
                case ImportSource.ISBN:
                    ImportURL(source);
                    break;
            }
        }

        public static List<string> ChooseFiles()
        {
            OpenFileDialog openFileDialog = new()
            {
                AddExtension = false,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == false) return new();
            return openFileDialog.FileNames.ToList();
        }

        public static void ImportManual()
        {
            CodexEditWindow editWindow = new(new CodexEditViewModel(null));
            editWindow.ShowDialog();
            editWindow.Topmost = true;
        }

        public static void ImportURL(ImportSource source)
        {
            ImportURLViewModel importVM = new(source);
            ImportURLWindow window = new(importVM);
            importVM.Window = window;
            window.Show();
        }

        public static async Task ImportFilesAsync(List<string> paths, CodexCollection? targetCollection = null)
        {
            targetCollection ??= MainViewModel.CollectionVM.CurrentCollection;

            //filter out codices already in collection & banned paths
            IEnumerable<string> existingPaths = targetCollection.AllCodices.Select(codex => codex.Sources.Path);
            paths = paths
                .Except(existingPaths)
                .Except(targetCollection.Info.BanishedPaths)
                .ToList();

            var progressVM = ProgressViewModel.GetInstance();

            progressVM.TotalAmount = paths.Count;
            progressVM.ResetCounter();
            progressVM.Text = "Importing files";

            if (paths.Count == 0) return;

            List<Codex> newCodices = new();

            //make new codices synchronously so they all have a valid ID
            foreach (string path in paths)
            {
                ProgressViewModel.GlobalCancellationTokenSource.Token.ThrowIfCancellationRequested();

                Codex newCodex = new(targetCollection);
                newCodex.Sources.Path = path;
                newCodices.Add(newCodex);
                targetCollection.AllCodices.Add(newCodex);

                LogEntry logEntry = new(Severity.Info, $"Importing {path}");
                progressVM.IncrementCounter();
                progressVM.AddLogEntry(logEntry);
            }

            MainViewModel.CollectionVM.CurrentCollection.Save();

            await FinishImport(newCodices);
        }

        public static async Task FinishImport(List<Codex> newCodices)
        {
            try
            {
                await CodexViewModel.StartGetMetaDataProcess(newCodices);
                await CoverService.GetCover(newCodices);
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warn("Import has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
                return;
            }

            foreach (Codex codex in newCodices)
            {
                codex.RefreshThumbnail();
            }
        }
    }
}
