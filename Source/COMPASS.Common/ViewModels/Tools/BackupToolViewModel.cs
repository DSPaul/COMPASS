using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Tools.Logging;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;

namespace COMPASS.Common.ViewModels.Tools;

public class BackupToolViewModel : ViewModelBase, IToolViewModel
{
    private readonly IApplicationDataService _applicationDataService;
    private readonly IFilesService _filesService;
    private readonly IImportExportService _importExportService;
    private readonly INotificationService _notificationService;
    private readonly CollectionManager _collectionManager;
    private readonly ILogger _logger;

    public BackupToolViewModel(
        IApplicationDataService applicationDataService,
        IFilesService filesService,
        IImportExportService importExportService,
        INotificationService notificationService,
        CollectionManager collectionManager,
        ILogger logger)
    {
        _applicationDataService = applicationDataService;
        _filesService = filesService;
        _importExportService = importExportService;
        _notificationService = notificationService;
        _collectionManager = collectionManager;
        _logger = logger;
    }
    
    #region IToolViewModel
    
    public string Name => "Backup & Restore";    
    
    #endregion

    private AsyncRelayCommand? _backupLocalFilesCommand;
    public AsyncRelayCommand BackupLocalFilesCommand => _backupLocalFilesCommand ??= new(BackupLocalFiles);
    private async Task BackupLocalFiles()
    {
        var saveFile = await _filesService.SaveFileAsync(new()
        {
            FileTypeChoices = [_filesService.ZipExtensionFilter]
        });

        if (saveFile != null)
        {
            string targetPath = saveFile.Path.LocalPath;
            saveFile.Dispose();
            LoadingWindow loadingWindow = new("Compressing to Zip File");
            loadingWindow.Show(WindowManager.ActiveWindow);

            //save first
            _collectionManager.SaveAllCollections();

            await Task.Run(() => _importExportService.CompressUserDataToZip(targetPath));

            loadingWindow.Close();
        }
    }

    private AsyncRelayCommand? _restoreBackupCommand;
    public AsyncRelayCommand RestoreBackupCommand => _restoreBackupCommand ??= new(RestoreBackup);
    private async Task RestoreBackup()
    {
        var files = await _filesService.OpenFilesAsync(new()
        {
            FileTypeFilter = [_filesService.ZipExtensionFilter]
        });

        if (files.Any())
        {
            using var file = files.Single();
            string targetPath = file.Path.LocalPath;
            LoadingWindow loadingWindow = new("Restoring Backup");
            loadingWindow.Show(WindowManager.ActiveWindow);

            await ExtractZip(targetPath);
            loadingWindow?.Close();

            //Restart the app with the new data
            var notfication = new Notification("Backup restored", "The backup has been restored. COMPASS will now restart.");
            await _notificationService.ShowDialog(notfication);
            ApplicationService.Restart(false);
        }
    }

    private async Task ExtractZip(string sourcePath)
    {
        if (!Path.Exists(sourcePath))
        {
            _logger.Warn("Cannot extract sourcePath as it does not exit");
            return;
        }
        
        var progressVm = ProgressViewModel.GetInstance();
        progressVm.Clear();

        var options = new ReaderOptions()
        {
            Progress = progressVm
        };
        await using var archive = await ZipArchive.OpenAsyncArchive(sourcePath, options);
        await archive.WriteToDirectoryAsync(_applicationDataService.UserDataPath);
    }
}

[Factory]
public class BackupToolViewModelFactory(
    IApplicationDataService applicationDataService,
    IFilesService filesService,
    IImportExportService importExportService,
    INotificationService notificationService,
    CollectionManager collectionManager,
    ILogger logger)
{
    public BackupToolViewModel Create()
        => new(applicationDataService, filesService, importExportService, notificationService, collectionManager, logger);
}