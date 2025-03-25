using CommunityToolkit.Mvvm.Input;

namespace COMPASS.Common.Interfaces
{
    public interface IConfirmable
    {
        RelayCommand CancelCommand { get; }

        RelayCommand ConfirmCommand { get; }
    }
}
