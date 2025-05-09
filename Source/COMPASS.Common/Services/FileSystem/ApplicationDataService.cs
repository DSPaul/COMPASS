using System;
using System.IO;
using System.Threading.Tasks;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.Services.FileSystem;

public class ApplicationDataService : IApplicationDataService
{
    private readonly IEnvironmentVarsService _envVarsService;
    private readonly INotificationService _notificationService;
    
    const string ROOT_DIRECTORY_NAME = "COMPASS";
    
    public ApplicationDataService(
        IEnvironmentVarsService envVarsService,
        INotificationService notificationService)
    {
        _envVarsService = envVarsService;
        _notificationService = notificationService;
    }
    
    public async Task<bool> UpdateRootDirectory(string newPath)
    {
        if (string.IsNullOrWhiteSpace(newPath) || !Path.Exists(newPath)) { return false; }

        //make sure the new folder ends on /COMPASS
        string folderName = new DirectoryInfo(newPath).Name;
        if (folderName != ROOT_DIRECTORY_NAME)
        {
            newPath = Path.Combine(newPath, ROOT_DIRECTORY_NAME);
            try
            {
                Directory.CreateDirectory(newPath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create the COMPASS folder at new data path location {newPath}", ex);
                return false;
            }
        }

        //check if the path is actually different
        if (newPath == _envVarsService.CompassDataPath)
        {
            return false;
        }

        //If there is existing data, Give users the choice between moving or copying
        var vm = new ChangeDataLocationViewModel(newPath);
        if (Path.Exists(_envVarsService.CompassDataPath))
        {
            ModalWindow window = new(vm);
            await window.ShowDialog(App.MainWindow);
        }
        //If not, just change over
        else
        {
            vm.ChangeToNewDataLocation();
        }

        return true;
    }
    
    /// <summary>
    /// If the existing codex path is inaccessible for any reason, prompt the user to pick another one
    /// </summary>
    /// <returns></returns>
    public async Task RequireNewCompassDataLocation(string msg)
    {
        Notification pickNewPath = new("Pick a location to save your data", msg, Severity.Warning)
        {
            ConfirmText = "Continue"
        };
        await _notificationService.ShowDialog(pickNewPath);

        bool success = false;
        while (!success)
        {
            string? newPath = await IOService.PickFolder();
            if (!string.IsNullOrWhiteSpace(newPath) && Path.Exists(newPath))
            {
                success = await UpdateRootDirectory(newPath);
            }
            else
            {
                Notification notValid = new("Invalid path", $"{newPath} is not a valid path, please try again",
                    Severity.Warning);
                await _notificationService.ShowDialog(notValid);
            }
        }
    }
}