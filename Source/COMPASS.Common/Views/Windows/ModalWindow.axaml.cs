using Avalonia.Controls;
using COMPASS.Common.Interfaces;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class ModalWindow : Window
{
    public ModalWindow(IModalViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseAction = Close;
    }
}