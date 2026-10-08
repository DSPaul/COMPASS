using CommunityToolkit.Mvvm.Input;

namespace COMPASS.Infra.Avalonia.Modal
{
    public interface IConfirmable
    {
        IRelayCommand CancelCommand { get; }

        IRelayCommand ConfirmCommand { get; }
    }
}
