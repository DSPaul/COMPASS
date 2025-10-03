using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals.Edit;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.ViewModels.Import
{
    public class ImportViewModel : ViewModelBase
    {
        public ImportViewModel(string targetCollectionId)
        {
            _targetcollectionId =  targetCollectionId;
        }

        private readonly string _targetcollectionId;
        
        public async Task Import(ImportSource source) => await Import(source, _targetcollectionId);
        public static async Task Import(ImportSource source, string targetCollectionId)
        {
            List<string> pathsToImport;
            switch (source)
            {
                case ImportSource.File:
                    pathsToImport = await ChooseFiles();
                    await ImportFilesAsync(pathsToImport, targetCollectionId);
                    break;
                case ImportSource.Folder:
                    using (ImportFilesViewModel folderVM = new(targetCollectionId, autoImport: false))
                    {
                        await folderVM.Import();
                    }
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

        private static async Task<List<string>> ChooseFiles()
        {
            var filesService = ServiceResolver.Resolve<IFilesService>();

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

        private static async Task ImportManual()
        {
            CodexEditViewModel vm = new();
            ModalWindow editWindow = new(vm);
            await editWindow.ShowDialog(App.MainWindow);
        }

        private static async Task ImportURL(ImportSource source)
        {
            ImportURLViewModel importVM = new(source);
            ModalWindow window = new(importVM);
            await window.ShowDialog(App.MainWindow);
        }

        public static async Task ImportFilesAsync(IList<string> paths, string? targetCollectionId = null)
        {
            targetCollectionId ??= TabsViewModel.GetInstance().ActiveTab?.CollectionHandle.CollectionVM.Identifier 
                                   ?? throw new NoTabException("There is no open tab, so no collection to import the files to");

            using CollectionHandle targetCollectionHandle = CollectionManager.LoadCollection(targetCollectionId) 
                                                            ?? throw new LoadException(targetCollectionId);
            var targetCollection = targetCollectionHandle.CollectionVM.Collection;
            
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

            List<Codex> newCodices = [];

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

                Codex newCodex = CodexOperations.CreateNewCodex(targetCollection);
                newCodex.Sources.Path = path;
                newCodices.Add(newCodex);
                targetCollection.AllCodices.Add(newCodex);

                LogEntry logEntry = new(Severity.Info, $"Importing {path}");
                progressVM.IncrementCounter();
                progressVM.AddLogEntry(logEntry);
            }
            
            targetCollection.Save();
            
            //now get metadata and cover async
            try
            {
                await new CodexOperations(targetCollectionHandle).StartGetMetaDataProcess(newCodices);
                await CoverService.GetAndApplyCover(newCodices);
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
