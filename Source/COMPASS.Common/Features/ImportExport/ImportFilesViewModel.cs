using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Codices;
using COMPASS.Infra.Avalonia.Modal;
using COMPASS.Infra.Collections;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Notifications;
using COMPASS.Common.Domain;
using COMPASS.Infra.Avalonia.Validation;
using COMPASS.Common.Features.Tabs;
using COMPASS.Infra.Measuring;
using COMPASS.Infra.Progress;
using System.Diagnostics;
namespace COMPASS.Common.Features.ImportExport;

public class ImportFilesViewModel : ValidatableViewModelBase, IDisposable
{
    /// <summary>
    /// Minimum time between progress updates while scanning. Reporting every folder would
    /// flood the UI with notifications when walking an entire drive.
    /// </summary>
    private const int ScanReportIntervalMs = 200;

    private readonly ILogger _logger;
    private readonly IIOService _ioService;
    private readonly INotificationService _notificationService;
    private readonly ImportFolderViewModelFactory _importFolderDialogFactory;
    private readonly FolderFactory _folderFactory;
    private readonly CodexCollectionOperations _codexCollectionOperations;
    private readonly CodexOperations _codexOperations;
    private readonly CollectionManager _collectionManager;
    private readonly IActiveTabContext _activeTabContext;
    private readonly ProgressTrackingManager _progressTrackingManager;

    private readonly bool _autoImport;
    private readonly CollectionHandle _targetCollectionHandle;
    private CodexCollection _TargetCollection => _targetCollectionHandle.CollectionVM.Collection;
    
    #region CTOR
    
    public ImportFilesViewModel(
        ILogger logger,
        IIOService ioService, INotificationService notificationService, 
        ImportFolderViewModelFactory importFolderDialogFactory, 
        FolderFactory folderFactory,
        CodexCollectionOperations codexCollectionOperations,
        CodexOperations codexOperations,
        CollectionManager collectionManager,
        IActiveTabContext activeTabContext,
        ProgressTrackingManager progressTrackingManager,
        string targetCollectionId, bool autoImport)
    {
        _logger = logger;
        _ioService = ioService;
        _notificationService = notificationService;
        _importFolderDialogFactory = importFolderDialogFactory;
        _folderFactory = folderFactory;
        _codexCollectionOperations = codexCollectionOperations;
        _codexOperations = codexOperations;
        _collectionManager = collectionManager;
        _activeTabContext = activeTabContext;
        _progressTrackingManager = progressTrackingManager;
        
        var handle = _collectionManager.LoadCollection(targetCollectionId);
        if (handle != null)
        {
            _targetCollectionHandle = handle;
        }
        else
        {
            throw new LoadException(targetCollectionId, "Target Collection not found");
        }
        _autoImport = autoImport;
    }
    
    #endregion
    
    #region Possible Inputs
    
    /// <summary>
    /// Import files top level and in subfolders of these directories.
    /// </summary>
    public List<string> RecursiveDirectories { get; set; } = [];
        
    /// <summary>
    /// Import files top level in these directories.
    /// </summary>
    public List<string> NonRecursiveDirectories { get; set; } = [];
        
    /// <summary>
    /// Loose files to import
    /// </summary>
    public List<string> Files { get; set; } = [];
        
    /// <summary>
    /// Folders that have already been imported once before, for auto import
    /// </summary>
    public List<Folder> ExistingFolders { get; set; } = [];

    #endregion
    
    public async Task Import()
    {
        //if no files are given to import, don't
        if (!_autoImport && RecursiveDirectories.Count + NonRecursiveDirectories.Count + ExistingFolders.Count + Files.Count == 0)
        {
            bool success = await LetUserSelectFolders();
            if (!success) return;
        }

        IList<string> toImport;

        try
        {
            toImport = await GetPathsToImport();
        }
        catch (OperationCanceledException)
        {
            _logger.Info("Import was cancelled while scanning for files");
            return;
        }
        
        if (toImport.Any())
        {
            ImportFolderViewModel? folderImportDialogVm = await LetUserRefineImport(toImport);
            if (folderImportDialogVm is null) return;

            IList<string> filteredFiles = folderImportDialogVm.GetFilteredFiles(toImport);
            IList<Codex> importedCodices = await _codexCollectionOperations.ImportFilesAsync(filteredFiles, _targetCollectionHandle.CollectionVM.Identifier);

            //EditCodices opens the single edit window for 1 codex and bulk edit for several
            if (folderImportDialogVm.OpenEditWindowWhenDone && importedCodices.Any())
            {
                await _codexOperations.EditCodices(importedCodices.ToList());
            }
            else if (!_autoImport) //if we don't open edit window, at least select the newly added items
            {
                _activeTabContext.ActiveTab?.SelectCodices(importedCodices);
            }
        }
        else if (!_autoImport)
        {
            Notification noFilesFound = new("No files found", "The selected folder did not contain any files.");
            await _notificationService.ShowDialog(noFilesFound);
        }
    }
    
    /// <summary>
    /// Lets a user select folders using a dialog 
    /// and stores them in RecursiveDirectories
    /// </summary>
    /// <returns> A bool indicating whether the user successfully chose a set of folders </returns>
    private async Task<bool> LetUserSelectFolders()
    {
        var selectedPaths = await _ioService.TryPickFolders().ConfigureAwait(false);
        RecursiveDirectories = [.. selectedPaths];
        return selectedPaths.Any();
    }
    
    /// <summary>
    /// Get a list of all the file paths that are not banned
    /// because of banishment or due to file extension preference
    /// That are either in FileName or in a folder in RecursiveDirectories
    /// </summary>
    /// <remarks>
    /// Walking a full drive can take minutes, so the scan runs on a background thread
    /// behind a progress tracker that shows the current folder and can be cancelled.
    /// </remarks>
    private async Task<IList<string>> GetPathsToImport()
    {
        ProgressTracker scanTracker = new(Quantities.Items("Folders"));
        IList<string> toImport = [];

        await _progressTrackingManager.RunAsync(scanTracker, "Scanning for files...",
            async (tracker, cancellationToken) =>
            {
                toImport = await Task.Run(() => FindFilesToImport(tracker, cancellationToken), cancellationToken);
            });

        return toImport;
    }

    private IList<string> FindFilesToImport(ProgressTracker tracker, CancellationToken cancellationToken)
    {
        Stopwatch reportThrottle = Stopwatch.StartNew();

        // 1. Unroll the recursive folders
        List<string> discoveredDirectories = [];
        Queue<string> toSearch = new(RecursiveDirectories);
        while (toSearch.Any())
        {
            cancellationToken.ThrowIfCancellationRequested();

            string currentFolder = toSearch.Dequeue();

            if (!Directory.Exists(currentFolder)) continue;
            
            discoveredDirectories.Add(currentFolder);

            foreach (string dir in _ioService.TryGetDirectories(currentFolder))
            {
                toSearch.Enqueue(dir);
            }

            if (reportThrottle.ElapsedMilliseconds >= ScanReportIntervalMs)
            {
                reportThrottle.Restart();
                tracker.Report(ProgressReports.Status($"{discoveredDirectories.Count:N0} folders scanned"));
            }
        }

        //2. Build a list with all the files to import
        List<string> toImport = [..Files];
        List<string> directoriesToSearch = 
            discoveredDirectories
            .Concat(NonRecursiveDirectories)
            .Concat(ExistingFolders.Flatten().Select(f => f.FullPath))
            .ToList();

        for (int directoryIndex = 0; directoryIndex < directoriesToSearch.Count; directoryIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string currentFolder = directoriesToSearch[directoryIndex];
            toImport.AddRange(_ioService.TryGetFilesInFolder(currentFolder));

            bool isLastFolder = directoryIndex == directoriesToSearch.Count - 1;

            //Now that the amount of folders is known, the bar can show how far along the file scan is
            if (isLastFolder || reportThrottle.ElapsedMilliseconds >= ScanReportIntervalMs)
            {
                reportThrottle.Restart();
                tracker.Report(ProgressReports.XOutOfY(directoryIndex + 1, directoriesToSearch.Count));
                tracker.Report(ProgressReports.Status($"{toImport.Count:N0} files found"));
            }
        }

        //3. Filter out doubles and banished paths
        tracker.Report(ProgressReports.Status($"Checking {toImport.Count:N0} files..."));
        return toImport.Distinct().Where(path => !PathUtils.MatchesAnyGlob(path, _TargetCollection.Info.BanishedPaths)).ToList();
    }

    /// <summary>
    /// Shows an ImportFolder dialog if certain conditions are met.
    /// Returns null when the user cancels the dialog.
    /// </summary>
    /// <returns></returns>
    private async Task<ImportFolderViewModel?> LetUserRefineImport(IList<string> allFilesToImport)
    {
        IList<Folder> folders = RecursiveDirectories.Select(_folderFactory.Create)
                                                    .Concat(ExistingFolders)
                                                    .ToList();
        
        var folderImportDialogVm = _importFolderDialogFactory.Create(_autoImport, _TargetCollection.Info, folders, allFilesToImport);
        
        if (!folderImportDialogVm.ShouldShowDialog)
        {
            return folderImportDialogVm;
        }

        await WindowManager.OpenModal(folderImportDialogVm);
        return folderImportDialogVm.Finished ? folderImportDialogVm : null;
    }

    public void Dispose()
    {
        _targetCollectionHandle.Dispose();
    }
}

    [Factory]
    public class ImportFilesViewModelFactory(
        ILogger logger,
        IIOService ioService, INotificationService notificationService, 
        ImportFolderViewModelFactory importFolderDialogFactory,
        Lazy<TabsViewModel> tabsViewModel,
        FolderFactory folderFactory,
        CodexCollectionOperations codexCollectionOperations,
        CodexOperations codexOperations,
        Lazy<CollectionManager> collectionManager,
        IActiveTabContext activeTabContext,
        ProgressTrackingManager progressTrackingManager)
    {
        public ImportFilesViewModel Create(bool autoImport)
        {
            //Lazy breaks the factory -> TabsViewModel -> CollectionManager -> factory cycle:
            //resolved here at runtime, long after the container is built
            var targetCollectionId = tabsViewModel.Value.ActiveTab?.CollectionVM.Identifier ??
                 throw new NoTabException("There is no open tab, so no collection to import the files to");

            return Create(targetCollectionId, autoImport);
        }

        public ImportFilesViewModel Create(string targetCollectionId, bool autoImport)
        {
            return new ImportFilesViewModel(
                logger, ioService, notificationService, 
                importFolderDialogFactory, folderFactory,
                codexCollectionOperations, codexOperations, collectionManager.Value, activeTabContext,
                progressTrackingManager, targetCollectionId, autoImport);
        }
    }