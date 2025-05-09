using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;

namespace COMPASS.Common.ViewModels.Modals;

public class ChangeDataLocationViewModel : ViewModelBase, IModalViewModel
{
    private string _currentDataLocation;
    private string _newDataLocation;

    private readonly IEnvironmentVarsService _envVarsService;
    private readonly ICodexCollectionStorageService _collectionStorageService;
    private readonly INotificationService _notificationService;

    public ChangeDataLocationViewModel(string newDataLocation)
    {
        _envVarsService = ServiceResolver.Resolve<IEnvironmentVarsService>();
        _collectionStorageService = ServiceResolver.Resolve<ICodexCollectionStorageService>();
        _notificationService = ServiceResolver.Resolve<INotificationService>();

        _currentDataLocation = _envVarsService.CompassDataPath;
        _newDataLocation = newDataLocation;
    }

    public string CurrentDataLocation
    {
        get => _currentDataLocation;
        set => SetProperty(ref _currentDataLocation, value);
    }

    public string NewDataLocation
    {
        get => _newDataLocation;
        set => SetProperty(ref _newDataLocation, value);
    }

    #region Methods & Commands

    private AsyncRelayCommand? _moveToNewDataLocationCommand;
    public AsyncRelayCommand MoveToNewDataLocationCommand => _moveToNewDataLocationCommand ??= new(MoveToNewDataLocation);
    private async Task MoveToNewDataLocation()
    {
        CloseAction();
        
        bool success = await IOService.CopyDataAsync(CurrentDataLocation, NewDataLocation);

        if (success)
        {
            await DeleteDataLocation();
        }
        else
        {
            //TODO could show a notification that it failed
        }        
    }

    private AsyncRelayCommand? _copyToNewDataLocationCommand;
    public AsyncRelayCommand CopyToNewDataLocationCommand => _copyToNewDataLocationCommand ??= new(CopyToNewDataLocation);
    private async Task CopyToNewDataLocation()
    {
        CloseAction();
        
        bool success = await IOService.CopyDataAsync(CurrentDataLocation, NewDataLocation);
        if (success)
        {
            ChangeToNewDataLocation();
        }
    }

    private RelayCommand? _changeToNewDataLocationCommand;
    public RelayCommand ChangeToNewDataLocationCommand => _changeToNewDataLocationCommand ??= new(ChangeToNewDataLocation);

    /// <summary>
    /// Sets the data path to <see cref="NewDataLocation"/> and restarts the app
    /// </summary>
    public void ChangeToNewDataLocation()
    {
        CloseAction();
        
        _collectionStorageService.Save(MainViewModel.CollectionVM.CurrentCollection);

        _envVarsService.CompassDataPath = NewDataLocation;

        Notification changeSuccessful = new("Data path changed successfully",
            $"Data path was successfully changed to {NewDataLocation}. COMPASS will now restart.");
        ServiceResolver.Resolve<INotificationService>().ShowDialog(changeSuccessful);
        
        Utils.Restart(false);
    }

    private AsyncRelayCommand? _deleteDataCommand;
    public AsyncRelayCommand DeleteDataCommand => _deleteDataCommand ??= new(DeleteDataLocation);
    private async Task DeleteDataLocation()
    {
        CloseAction();
        
        try
        {
            var notification = Notification.AreYouSureNotification;
            notification.Body = $"Are you sure you want to delete all data from {CurrentDataLocation}?";
            
            await _notificationService.ShowDialog(notification);

            if (notification.Result == NotificationAction.Cancel)
            {
                return;
            }
            
            Directory.Delete(CurrentDataLocation, true);
        }
        catch (Exception ex)
        {
            Logger.Error("could not delete all data", ex);
        }

        ChangeToNewDataLocation();
    }

    #endregion

    #region IModalWindow

    public string WindowTitle => "Change Data Location";
    public int? WindowWidth { get; } = 700;
    public int? WindowHeight { get; } = 400;
    public Action CloseAction { get; set; } = () => { };

    #endregion
}