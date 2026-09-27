using CommunityToolkit.Mvvm.Input;

namespace COMPASS.Infra.Avalonia.Mvvm
{
    public interface IConfirmable
    {
        IRelayCommand CancelCommand { get; }

        IRelayCommand ConfirmCommand { get; }
    }
}
