using System;
using Avalonia.Controls;
using COMPASS.Common.Interfaces;

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
    }
}