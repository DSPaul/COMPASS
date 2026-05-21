using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Services.FileSystem;

public class ApplicationDataService(
    IIOService ioService,
    INotificationService notificationService,
    ILogger logger,
    IPreferencesService preferencesService)
    : IApplicationDataService
{
    private const string RedirectFileName = "data_location.redirect";

    /// <summary>
    /// Default home for user data (collections, preferences) when no redirect is configured.
    /// Users may redirect this to a cloud-synced folder.
    /// </summary>
    /// 
    public static readonly string DefaultUserDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.DIR_ROOT);

    private string RedirectFilePath => Path.Combine(IApplicationDataService.ApplicationDataPath, RedirectFileName);

    public string UserDataPath
    {
        get
        {
            if (File.Exists(RedirectFilePath))
            {
                string redirectedPath = File.ReadAllText(RedirectFilePath).Trim();
                if (Path.Exists(redirectedPath))
                    return redirectedPath;
            }
            return DefaultUserDataPath;
        }
        set
        {
            Directory.CreateDirectory(IApplicationDataService.ApplicationDataPath);

            if (value == DefaultUserDataPath)
            {
                if (File.Exists(RedirectFilePath))
                {
                    File.Delete(RedirectFilePath);
                }
            }
            else
            {
                File.WriteAllText(RedirectFilePath, value);
            }
        }
    }

    /// <summary>
    /// One-time migration from the previous Avalonia version, which persisted the data
    /// path in the <c>COMPASS_DATA_PATH</c> environment variable. If the variable is set
    /// and no redirect file exists yet, the redirect file is written and the variable is
    /// cleared so it has no effect on future launches.
    /// </summary>
    public void MigrateFromV1()
    {
        const string LegacyEnvVarKey = "COMPASS_DATA_PATH";

        // If redirect is already in the new Local location, nothing to do.
        if (File.Exists(RedirectFilePath))
        {
            return;
        }

        // User path could have been in env variable
        string? legacyPath = Environment.GetEnvironmentVariable(LegacyEnvVarKey);
        if (Path.Exists(legacyPath))
        {
            UserDataPath = legacyPath;
        }

        if (string.IsNullOrEmpty(legacyPath))
        {
            // Remove the variable so it doesn't interfere on future launches.
            try 
            {
                Environment.SetEnvironmentVariable(LegacyEnvVarKey, null, EnvironmentVariableTarget.User); 
            }
            catch (PlatformNotSupportedException) { }
        }

        //Thumbnails moved, delete old thumbnail folder if it exists
        var collectionsPath = Path.Combine(UserDataPath, Constants.DIR_COLLECTIONS);
        if (!Path.Exists(collectionsPath))
        {
            return;
        }

        foreach(string colletionDir in Directory.GetDirectories(collectionsPath))
        {
            var oldThumbnailPath = Path.Combine(colletionDir, "Thumbnails");
            if (Directory.Exists(oldThumbnailPath))
            {
                try
                {
                    Directory.Delete(oldThumbnailPath, true);
                }
                catch (Exception ex)
                {
                    logger.Warn($"Failed to delete old thumbnail folder at {oldThumbnailPath} during migration", ex);
                }
            }
        }
    }

    public async Task<bool> UpdateUserDataPath(string newPath)
    {
        if (string.IsNullOrWhiteSpace(newPath)) { return false; }

        //make sure the new folder ends on /COMPASS
        string folderName = new DirectoryInfo(newPath).Name;
        if (folderName != Constants.DIR_ROOT)
        {
            newPath = Path.Combine(newPath, Constants.DIR_ROOT);
            try
            {
                Directory.CreateDirectory(newPath);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to create the {Constants.DIR_ROOT} folder at new data path location {newPath}", ex);
                return false;
            }
        }

        //check if the path is actually different
        if (newPath == UserDataPath)
        {
            return false;
        }

        ChangeDataLocationActions action = ChangeDataLocationActions.Leave;
        if (Path.Exists(UserDataPath))
        {
            //If there is existing data, Give users the choice between moving or copying
            var vm = new ChangeDataLocationViewModel(UserDataPath, newPath);
            await WindowManager.OpenModal(vm);
            action = vm.Result;
        }
        
        if (action == ChangeDataLocationActions.Cancel)
        {
            return false;
        }

        //Save data before moving files around, just in case
        CollectionManager.SaveAllCollections();
        preferencesService.SavePreferences();

        //Move data to new location if chosen
        if (action == ChangeDataLocationActions.Move || 
            action == ChangeDataLocationActions.Copy)
        {
            string oldPath = UserDataPath;
            bool success = await ioService.CopyDataAsync(oldPath, newPath);
            if (!success)
            {
                Notification notMoved = new("Move failed", $"Failed to move data to {newPath}, please try again or choose a different location",
                    Severity.Error);
                await notificationService.ShowDialog(notMoved);
                return false;
            }
            RebasePathsInDataFiles(oldPath, newPath);
        }

        //If wipe, double check deletion confirm
        if (action == ChangeDataLocationActions.Wipe)
        {
            var notification = Notification.AreYouSureNotification;
            notification.Body = $"Are you sure you want to delete all data from {UserDataPath}?";

            await notificationService.ShowDialog(notification);

            if (notification.Result == NotificationAction.Cancel)
            {
                return false;
            }
        }

        //Delete data in previous location if chosen
        if (action == ChangeDataLocationActions.Wipe || 
            action == ChangeDataLocationActions.Move)
        {
            try
            {
                Directory.Delete(UserDataPath, true);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to delete data at the previous data path location {UserDataPath}", ex);
                Notification notWiped = new("Deleting old data failed", $"Failed to delete the data at the previous location {UserDataPath}",
                    Severity.Error);
                notWiped.Details = ex.Message;
                await notificationService.ShowDialog(notWiped);
                return false;
            }
        }

        //Do the actual change
        UserDataPath = newPath;

        Notification changeSuccessful = new("User data location changed successfully",
            $"The save location of COMPASS' data was successfully changed to {newPath}. COMPASS will now restart.");
        await notificationService.ShowDialog(changeSuccessful);

        //Now that datapath has been changed, don't save on close because it would save to new location
        //If we are leaving the old data behind, we don't what that
        //and if we bring the old data along, it has already been copied so redundant
        MainViewModel.SaveOnClose = false;

        ApplicationService.Restart(false);

        return true;
    }
    
    public async Task<bool> ResetUserDataPath() => await UpdateUserDataPath(DefaultUserDataPath);

    /// <summary>
    /// After data is copied to a new location, replaces all occurrences of the old
    /// <paramref name="oldBasePath"/> with <paramref name="newBasePath"/> inside every
    /// XML data file that was copied, so that absolute paths like <c>CoverArtPath</c>
    /// and user-file <c>Sources.Path</c> values point to the new location on the
    /// next launch.
    /// </summary>
    private void RebasePathsInDataFiles(string oldBasePath, string newBasePath)
    {
        string collectionsPath = Path.Combine(newBasePath, Constants.DIR_COLLECTIONS);
        if (!Directory.Exists(collectionsPath)) return;

        foreach (string xmlFile in Directory.GetFiles(collectionsPath, "*.xml", SearchOption.AllDirectories))
        {
            try
            {
                string content = File.ReadAllText(xmlFile);
                if (content.Contains(oldBasePath))
                {
                    File.WriteAllText(xmlFile, content.Replace(oldBasePath, newBasePath));
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to rebase paths in {xmlFile} during data migration", ex);
            }
        }
    }

    /// <summary>
    /// If the existing codex path is inaccessible for any reason, prompt the user to pick another one
    /// </summary>
    /// <returns></returns>
    public async Task RequireNewUserDataLocation(string msg)
    {
        Notification pickNewPath = new("Pick a location to save your data", msg, Severity.Warning)
        {
            ConfirmText = "Continue"
        };
        await notificationService.ShowDialog(pickNewPath);

        bool success = false;
        while (!success)
        {
            string? newPath = await ioService.PickFolder();
            if (!string.IsNullOrWhiteSpace(newPath) && Path.Exists(newPath))
            {
                success = await UpdateUserDataPath(newPath);
            }
            else
            {
                Notification notValid = new("Invalid path", $"{newPath} is not a valid path, please try again",
                    Severity.Warning);
                await notificationService.ShowDialog(notValid);
            }
        }
    }
}