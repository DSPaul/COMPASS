using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.Views.Windows;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;

namespace COMPASS.Common.ViewModels.Tools;

public class BackupToolViewModel : ViewModelBase, IToolViewModel
{
    private LoadingWindow? _lw;
    private readonly PreferencesService _preferencesService;
    private readonly IEnvironmentVarsService _environmentVarsService;

    public BackupToolViewModel()
    {
        _preferencesService = PreferencesService.GetInstance();
        _environmentVarsService = ServiceResolver.Resolve<IEnvironmentVarsService>();
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
            string targetPath = saveFile.Path.AbsolutePath;
            saveFile.Dispose();
            _lw = new("Compressing to Zip File");
            _lw.Show();

            //save first
            CollectionManager.SaveAllCollections();

            var collectionStorageService = ServiceResolver.ResolveKeyed<ICodexCollectionStorageService>(StorageStrategy.Xml);
            await Task.Run(() => collectionStorageService.CompressUserDataToZip(targetPath));

            _lw.Close();
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
            string targetPath = file.Path.AbsolutePath;
            _lw = new("Restoring Backup");
            _lw.Show();

            await Task.Run(() => ExtractZip(targetPath));

            //TODO should probably just restart after restore
            
            //restore collection that was open
            using var defaultCollectionVM = await CollectionManager.GetOrCreateInitialCollectionVM();
            var tabsVm = TabsViewModel.GetInstance();
            if (tabsVm.ActiveTab == null)
            {
                tabsVm.CreateTab();
            }
            else
            {
                await tabsVm.ActiveTab.ChangeToCollection(defaultCollectionVM);
            }
            _lw?.Close();
        }
    }

    private void ExtractZip(string sourcePath)
    {
        if (!Path.Exists(sourcePath))
        {
            Logger.Warn($"Cannot extract sourcePath as it does not exit");
            return;
        }

        using ZipArchive archive = ZipArchive.Open(sourcePath);
        archive.ExtractToDirectory(_environmentVarsService.CompassDataPath);
    }
}