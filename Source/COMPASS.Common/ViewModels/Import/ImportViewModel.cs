using COMPASS.Common.Exceptions;
using COMPASS.Common.DependencyInjection;
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
        private readonly CodexEditViewModelFactory _codexEditViewModelFactory;
        private readonly ImportFilesViewModelFactory _importFilesViewModelFactory;
        private readonly ImportURLViewModelFactory _importURLViewModelFactory;
        private readonly ISBNScannerViewModelFactory _isbnScannerViewModelFactory;
        private readonly IFilesService _filesService;

        public ImportViewModel(
            CodexEditViewModelFactory codexEditViewModelFactory, 
            ImportFilesViewModelFactory importFilesViewModelFactory, 
            ImportURLViewModelFactory importURLViewModelFactory,
            ISBNScannerViewModelFactory isbnScannerViewModelFactory,
            IFilesService filesService,
            string targetCollectionId)
        {
            _codexEditViewModelFactory = codexEditViewModelFactory;
            _importURLViewModelFactory = importURLViewModelFactory;
            _importFilesViewModelFactory = importFilesViewModelFactory;
            _isbnScannerViewModelFactory = isbnScannerViewModelFactory;
            _filesService = filesService;
            _targetcollectionId = targetCollectionId;
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
                    using (ImportFilesViewModel folderVM = _importFilesViewModelFactory.Create(targetCollectionId, autoImport: false))
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

        private async Task<List<string>> ChooseFiles()
        {
            var files = await _filesService.OpenFilesAsync(new()
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
            CodexEditViewModel vm = _codexEditViewModelFactory.Create(CodexOperations.CreateNewCodex(handle!.CollectionVM.Collection), createNew: true);
            await WindowManager.OpenModal(vm);
        }

        private async Task ImportURL(ImportSource source)
        {
            ImportURLViewModel importVM = _importURLViewModelFactory.Create(source);
            await WindowManager.OpenModal(importVM);
        }

        private async Task ImportISBN()
        {
            ISBNScannerViewModel importVM = _isbnScannerViewModelFactory.Create();
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

    [Factory]
    public class ImportViewModelFactory(
        CodexEditViewModelFactory codexEditViewModelFactory,
        ImportFilesViewModelFactory importFilesViewModelFactory,
        ImportURLViewModelFactory importURLViewModelFactory,
        ISBNScannerViewModelFactory isbnScannerViewModelFactory,
        IFilesService filesService)
    {
        public ImportViewModel Create(string targetCollectionId)
            => new(codexEditViewModelFactory, importFilesViewModelFactory, importURLViewModelFactory, isbnScannerViewModelFactory, filesService, targetCollectionId);
    }
}
