using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.ViewModels.Modals;

public class ChangeDataLocationViewModel : ViewModelBase, IModalViewModel
{
    public ChangeDataLocationActions Result { get; private set; }

    public ChangeDataLocationViewModel(string currentLocation, string newDataLocation)
    {
        CurrentDataLocation = currentLocation;
        NewDataLocation = newDataLocation;
    }

    public string CurrentDataLocation { get; }

    public string NewDataLocation { get; }

    #region Methods & Commands

    private AsyncRelayCommand? _moveToNewDataLocationCommand;
    public AsyncRelayCommand MoveToNewDataLocationCommand => _moveToNewDataLocationCommand ??= new(MoveToNewDataLocation);
    private async Task MoveToNewDataLocation()
    {
        Result = ChangeDataLocationActions.Move;
        CloseAction();
    }

    private AsyncRelayCommand? _copyToNewDataLocationCommand;
    public AsyncRelayCommand CopyToNewDataLocationCommand => _copyToNewDataLocationCommand ??= new(CopyToNewDataLocation);
    private async Task CopyToNewDataLocation()
    {
        Result = ChangeDataLocationActions.Copy;
        CloseAction();
    }

    private RelayCommand? _changeToNewDataLocationCommand;
    public RelayCommand ChangeToNewDataLocationCommand => _changeToNewDataLocationCommand ??= new(ChangeToNewDataLocation);

    /// <summary>
    /// Sets the data path to <see cref="NewDataLocation"/> and restarts the app
    /// </summary>
    public void ChangeToNewDataLocation()
    {
        Result = ChangeDataLocationActions.Leave;
        CloseAction();
    }

    private AsyncRelayCommand? _deleteDataCommand;
    public AsyncRelayCommand DeleteDataCommand => _deleteDataCommand ??= new(DeleteDataLocation);
    private async Task DeleteDataLocation()
    {
        Result = ChangeDataLocationActions.Wipe;
        CloseAction();
    }

    #endregion

    #region IModalWindow

    public string WindowTitle => "Change Data Location";
    public Action CloseAction { get; set; } = () => { };

    #endregion
}