using CommunityToolkit.Mvvm.Input;

namespace COMPASS.Common.Interfaces
{
    public interface IConfirmable
    {
        IRelayCommand CancelCommand { get; }

        IRelayCommand ConfirmCommand { get; }
    }
}
