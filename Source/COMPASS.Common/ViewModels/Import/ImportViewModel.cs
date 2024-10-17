using Autofac;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels.Import
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
                    pathsToImport = await ChooseFiles();
                    await ImportFilesAsync(pathsToImport, targetCollection);
                    break;
                case ImportSource.Folder:
                    ImportFolderViewModel importFolderVM = new(targetCollection, manuallyTriggered: true);
                    await importFolderVM.Import();
                    break;
                case ImportSource.Manual:
                    await ImportManual();
                    break;
                case ImportSource.GmBinder:
                case ImportSource.Homebrewery:
                case ImportSource.GoogleDrive:
                case ImportSource.GenericURL:
                case ImportSource.ISBN:
                    await ImportURL(source);
                    break;
            }
        }

        public static async Task<List<string>> ChooseFiles()
        {
            var filesService = App.Container.Resolve<IFilesService>();

            var files = await filesService.OpenFilesAsync(new()
            {
                AllowMultiple = true,
            });

            if (!files.Any()) return [];

            var paths = files.Select(f => f.Path.AbsolutePath).ToList();

            foreach (var file in files)
            {
                file.Dispose();
            }

            return paths;
        }

        public static async Task ImportManual()
        {
            CodexEditWindow editWindow = new(new CodexEditViewModel(null));
            editWindow.Topmost = true;
            await editWindow.ShowDialog(App.MainWindow);
        }

        public static async Task ImportURL(ImportSource source)
        {
            ImportURLViewModel importVM = new(source);
            ImportURLWindow window = new(importVM);
            importVM.Window = window;
            await window.ShowDialog(App.MainWindow);
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
                try
                {
                    ProgressViewModel.GlobalCancellationTokenSource.Token.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    ProgressViewModel.GetInstance().ConfirmCancellation();
                    break;
                }

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
