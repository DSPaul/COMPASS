using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Import;

public class ImportFilesViewModel : ViewModelBase, IDisposable
{
    private readonly bool _autoImport;
    private readonly CollectionHandle _targetCollectionHandle;
    private CodexCollection _TargetCollection => _targetCollectionHandle.CollectionVM.Collection;
    
    #region CTOR
    
    public ImportFilesViewModel(bool autoImport) : 
        this(TabsViewModel.GetInstance().ActiveTab?.CollectionVM.Identifier ?? 
             throw new NoTabException("There is no open tab, so no collection to import the files to"), 
            autoImport) { }
    public ImportFilesViewModel(string targetCollectionId, bool autoImport)
    {
        var handle = CollectionManager.LoadCollection(targetCollectionId);
        if (handle != null)
        {
            _targetCollectionHandle = handle;
        }
        else
        {
            //TODO probably throw a custom exception to indicate the target could not be loaded
            throw new Exception("Target Collection not found");
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

        var toImport = GetPathsToImport();
        
        if (toImport.Any())
        {
            toImport = await LetUserFilterToImport(toImport);
            await ImportViewModel.ImportFilesAsync(toImport, _targetCollectionHandle.CollectionVM.Identifier);
        }
        else if (!_autoImport)
        {
            Notification noFilesFound = new("No files found", "The selected folder did not contain any files.");
            var windowedNotificationService = ServiceResolver.Resolve<INotificationService>();
            await windowedNotificationService.ShowDialog(noFilesFound);
        }
    }
    
    /// <summary>
    /// Lets a user select folders using a dialog 
    /// and stores them in RecursiveDirectories
    /// </summary>
    /// <returns> A bool indicating whether the user successfully chose a set of folders </returns>
    private async Task<bool> LetUserSelectFolders()
    {
        var selectedPaths = await IOService.TryPickFolders().ConfigureAwait(false);
        RecursiveDirectories = [.. selectedPaths];
        return selectedPaths.Any();
    }
    
    /// <summary>
    /// Get a list of all the file paths that are not banned
    /// because of banishment or due to file extension preference
    /// That are either in FileName or in a folder in RecursiveDirectories
    /// </summary>
    /// <returns></returns>
    private IList<string> GetPathsToImport()
    {
        // 1. Unroll the recursive folders
        List<string> discoveredDirectories = [];
        Queue<string> toSearch = new(RecursiveDirectories);
        while (toSearch.Any())
        {
            string currentFolder = toSearch.Dequeue();

            if (!Directory.Exists(currentFolder)) continue;
            
            discoveredDirectories.Add(currentFolder);
            IEnumerable<string> subfolders;
            try
            {
                subfolders = Directory.GetDirectories(currentFolder);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get subfolders of {currentFolder}", ex);
                subfolders = [];
            }
            foreach (string dir in subfolders)
            {
                toSearch.Enqueue(dir);
            }
        }

        //2. Build a list with all the files to import
        List<string> toImport = [..Files];
        var directoriesToSearch = 
            discoveredDirectories
            .Concat(NonRecursiveDirectories)
            .Concat(ExistingFolders.Flatten().Select(f => f.FullPath));
        foreach (var folder in directoriesToSearch)
        {
            toImport.AddRange(IOService.TryGetFilesInFolder(folder));
        }

        //3. Filter out doubles and banished paths
        return toImport.Distinct().Where(path => !IOService.MatchesAnyGlob(path, _TargetCollection.Info.BanishedPaths)).ToList();
    }

    /// <summary>
    /// Shows an ImportFolderWizard if certain conditions are met
    /// </summary>
    /// <returns></returns>
    private async Task<List<string>> LetUserFilterToImport(IList<string> allFilesToImport)
    {
        IList<Folder> folders = RecursiveDirectories.Select(f => new Folder(f))
                                                    .Concat(ExistingFolders)
                                                    .ToList();
        
        var folderImportWizardVm = new ImportFolderWizardVm(_autoImport, _TargetCollection.Info, folders, allFilesToImport);
        
        if (folderImportWizardVm.Steps.Any())
        {
            ModalWindow importFolderWindow = new(folderImportWizardVm);
            await importFolderWindow.ShowDialog(App.MainWindow).ConfigureAwait(false);

            if (!folderImportWizardVm.Finished)
            {
                return [];
            }
        }

        return folderImportWizardVm.GetFilteredFiles(allFilesToImport);
    }

    public void Dispose()
    {
        _targetCollectionHandle.Dispose();
    }
}