using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
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
        
        // Wait until the layout is ready
        ContentPresenter.Loaded += (_, _) => UpdateSizeBounds();
    }
    
    private void UpdateSizeBounds()
    {
        Visual? presenter = ContentPresenter.GetVisualChildren().FirstOrDefault(); //inner content presenter
        Control? modalView = presenter?.GetVisualChildren().OfType<Control>().FirstOrDefault();
        
        if (modalView == null) return;
        
        MinWidth = modalView.MinWidth;
        MinHeight = modalView.MinHeight;
        
        MaxWidth = modalView.MaxWidth;
        MaxHeight = modalView.MaxHeight;
        
        Width = modalView.Width;
        Height = modalView.Height;

        SizeToContent = SizeToContent.Manual; //don't resize modal after it's loaded
    }
}