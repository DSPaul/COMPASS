using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals.Edit;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Import
{
    public class ImportViewModel : ViewModelBase
    {
        public ImportViewModel(string targetCollectionId)
        {
            _targetcollectionId =  targetCollectionId;
        }

        private readonly string _targetcollectionId;
        
        public async Task Import(ImportSource source) => await Import(source, _targetcollectionId).ConfigureAwait(false);
        private async Task Import(ImportSource source, string targetCollectionId)
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
                    await ImportURL(source);
                    break;
                case ImportSource.ISBN:
                    await ImportISBN();
                    break;
            }
        }

        private static async Task<List<string>> ChooseFiles()
        {
            var filesService = ServiceResolver.Resolve<IFilesService>();

            var files = await filesService.OpenFilesAsync(new()
            {
                AllowMultiple = true,
            }).ConfigureAwait(false);

            if (!files.Any()) return [];

            var paths = files.Select(f => f.Path.LocalPath).ToList();

            foreach (var file in files)
            {
                file.Dispose();
            }

            return paths;
        }

        private async Task ImportManual()
        {
            using var handle = CollectionManager.LoadCollection(_targetcollectionId);
            CodexEditViewModel vm = new(CodexOperations.CreateNewCodex(handle!.CollectionVM.Collection), createNew: true);
            await WindowManager.OpenModal(vm);
        }

        private static async Task ImportURL(ImportSource source)
        {
            ImportURLViewModel importVM = new(source);
            await WindowManager.OpenModal(importVM);
        }

        private static async Task ImportISBN()
        {
            ISBNScannerViewModel importVM = new();
            await WindowManager.OpenModal(importVM);
        }

        public static async Task ImportFilesAsync(IList<string> paths, string? targetCollectionId = null)
        {
            var logger = ServiceResolver.Resolve<ILogger>();

            targetCollectionId ??= TabsViewModel.GetInstance().ActiveTab?.CollectionVM.Identifier 
                                   ?? throw new NoTabException("There is no open tab, so no collection to import the files to");

            using CollectionHandle targetCollectionHandle = CollectionManager.LoadCollection(targetCollectionId) 
                                                            ?? throw new LoadException(targetCollectionId);
            var targetCollection = targetCollectionHandle.CollectionVM.Collection;
            
            //filter out codices already in collection & banned paths
            IEnumerable<string> existingPaths = targetCollection.AllCodices.Select(codex => codex.Sources.Path);
            var sourceSets = paths
                .Except(existingPaths)
                .Except(targetCollection.Info.BanishedPaths)
                .Select(path => new SourceSet()
                {
                    Path = path,
                })
                .ToList();

            await CreateCodicesAsync(sourceSets, targetCollectionId);
        }

        public static async Task CreateCodicesAsync(IList<SourceSet> sourceSets, string? targetCollectionId = null)
        {
            var logger = ServiceResolver.Resolve<ILogger>();

            targetCollectionId ??= TabsViewModel.GetInstance().ActiveTab?.CollectionVM.Identifier
                                   ?? throw new NoTabException("There is no open tab, so no collection to import the items to");

            using CollectionHandle targetCollectionHandle = CollectionManager.LoadCollection(targetCollectionId)
                                                            ?? throw new LoadException(targetCollectionId);
            var targetCollection = targetCollectionHandle.CollectionVM.Collection;

            var progressVM = ProgressViewModel.GetInstance();

            progressVM.TotalAmount = sourceSets.Count;
            progressVM.ResetCounter();
            progressVM.Text = "Importing new items...";

            if (sourceSets.Count == 0) return;

            List<Codex> newCodices = [];

            //make new codices synchronously so they all have a valid ID
            foreach (var sourceSet in sourceSets)
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
                newCodex.Sources = sourceSet;
                newCodices.Add(newCodex);
                targetCollection.AllCodices.Add(newCodex);

                LogEntry logEntry = new(Severity.Info, $"Importing {sourceSet}");
                progressVM.IncrementCounter();
                progressVM.AddLogEntry(logEntry);
            }

            targetCollection.Save();

            //now get metadata and cover async
            try
            {
                await CodexOperations.StartGetMetaDataProcess(newCodices);
                await CoverService.GetAndApplyCover(newCodices);
            }
            catch (OperationCanceledException ex)
            {
                logger.Warn("Import has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
                return;
            }

            foreach (Codex codex in newCodices)
            {
                codex.NotifyCoverChanged();
            }
        }
    }
}
