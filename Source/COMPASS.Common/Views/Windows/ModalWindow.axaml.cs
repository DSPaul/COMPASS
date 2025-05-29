using System;
using Avalonia.Controls;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class ModalWindow : Window
{
    /// <summary>
    /// //DO NOT USE, FOR DESIGNER ONLY
    /// </summary>
    public ModalWindow()
    {
        throw new Exception("DO NOT USE PARAMETERLESS CONSTRUCTOR");
    }
    
    public ModalWindow(IModalViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseAction = Close;

        if (vm.SizeToContent == SizeToContent.Manual)
        {
            if(vm.WindowHeight > 0)
                Height = vm.WindowHeight.Value;
            if(vm.WindowWidth > 0)
                Width = vm.WindowWidth.Value;
        }
    }
}