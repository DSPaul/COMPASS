using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Tools;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;

namespace COMPASS.Common.ViewModels.Tools;

public class BackupToolViewModel : ViewModelBase, IToolViewModel
{
    private readonly IApplicationDataService _applicationDataService;

    public BackupToolViewModel()
    {
        _applicationDataService = ServiceResolver.Resolve<IApplicationDataService>();
    }
    
    #region IToolViewModel
    
    public string Name => "Backup & Restore";    
    
    #endregion

    private AsyncRelayCommand? _backupLocalFilesCommand;
    public AsyncRelayCommand BackupLocalFilesCommand => _backupLocalFilesCommand ??= new(BackupLocalFiles);
    private async Task BackupLocalFiles()
    {
        var filesService = ServiceResolver.Resolve<IFilesService>();
        var saveFile = await filesService.SaveFileAsync(new()
        {
            FileTypeChoices = [filesService.ZipExtensionFilter]
        });

        if (saveFile != null)
        {
            string targetPath = saveFile.Path.LocalPath;
            saveFile.Dispose();
            LoadingWindow loadingWindow = new("Compressing to Zip File");
            loadingWindow.Show(WindowManager.ActiveWindow);

            //save first
            CollectionManager.SaveAllCollections();

            var collectionStorageService = ServiceResolver.Resolve<IImportExportService>();
            await Task.Run(() => collectionStorageService.CompressUserDataToZip(targetPath));

            loadingWindow.Close();
        }
    }

    private AsyncRelayCommand? _restoreBackupCommand;
    public AsyncRelayCommand RestoreBackupCommand => _restoreBackupCommand ??= new(RestoreBackup);
    private async Task RestoreBackup()
    {
        var filesService = ServiceResolver.Resolve<IFilesService>();
        var files = await filesService.OpenFilesAsync(new()
        {
            FileTypeFilter = [filesService.ZipExtensionFilter]
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
            var notificationService = ServiceResolver.Resolve<INotificationService>();
            var notfication = new Notification("Backup restored", "The backup has been restored. COMPASS will now restart.");
            await notificationService.ShowDialog(notfication);
            ApplicationService.Restart(false);
        }
    }

    private async Task ExtractZip(string sourcePath)
    {
        if (!Path.Exists(sourcePath))
        {
            Logger.Warn("Cannot extract sourcePath as it does not exit");
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