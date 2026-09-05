using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Operations;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Modals.Edit;
using COMPASS.Common.ViewModels.Modals.Import;

namespace COMPASS.Common.ViewModels.Import
{
    public class ImportViewModel : ViewModelBase
    {
        private readonly CodexEditViewModelFactory _codexEditViewModelFactory;
        private readonly ImportFilesViewModelFactory _importFilesViewModelFactory;
        private readonly ImportURLViewModelFactory _importURLViewModelFactory;
        private readonly ISBNScannerViewModelFactory _isbnScannerViewModelFactory;
        private readonly IFilesService _filesService;
        private readonly CodexCollectionOperations _codexCollectionOperations;

        public ImportViewModel(
            CodexEditViewModelFactory codexEditViewModelFactory, 
            ImportFilesViewModelFactory importFilesViewModelFactory, 
            ImportURLViewModelFactory importURLViewModelFactory,
            ISBNScannerViewModelFactory isbnScannerViewModelFactory,
            IFilesService filesService,
            CodexCollectionOperations codexCollectionOperations,
            string targetCollectionId)
        {
            _codexEditViewModelFactory = codexEditViewModelFactory;
            _importURLViewModelFactory = importURLViewModelFactory;
            _importFilesViewModelFactory = importFilesViewModelFactory;
            _isbnScannerViewModelFactory = isbnScannerViewModelFactory;
            _filesService = filesService;
            _codexCollectionOperations = codexCollectionOperations;
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
                    await _codexCollectionOperations.ImportFilesAsync(pathsToImport, targetCollectionId);
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
            CodexEditViewModel vm = _codexEditViewModelFactory.Create(_codexCollectionOperations.CreateNewCodex(handle!.CollectionVM.Collection), createNew: true);
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
    }

    [Factory]
    public class ImportViewModelFactory(
        CodexEditViewModelFactory codexEditViewModelFactory,
        ImportFilesViewModelFactory importFilesViewModelFactory,
        ImportURLViewModelFactory importURLViewModelFactory,
        ISBNScannerViewModelFactory isbnScannerViewModelFactory,
        IFilesService filesService,
        CodexCollectionOperations codexCollectionOperations)
    {
        public ImportViewModel Create(string targetCollectionId)
            => new(codexEditViewModelFactory, importFilesViewModelFactory, importURLViewModelFactory, isbnScannerViewModelFactory, filesService, codexCollectionOperations, targetCollectionId);
    }
}
