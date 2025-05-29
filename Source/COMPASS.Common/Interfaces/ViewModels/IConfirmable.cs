using CommunityToolkit.Mvvm.Input;

namespace COMPASS.Common.Interfaces.ViewModels
{
    public interface IConfirmable
    {
        IRelayCommand CancelCommand { get; }

        IRelayCommand ConfirmCommand { get; }
    }
}
