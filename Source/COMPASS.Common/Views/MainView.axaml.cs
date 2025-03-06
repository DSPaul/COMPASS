using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Layouts;

namespace COMPASS.Common.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private async void UserControl_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.S:
                // Ctrl + S to search
                if (e.KeyModifiers == KeyModifiers.Control)
                {
                    Searchbar.Focus();
                    e.Handled = true;
                }
                break;

            case Key.I:
                // Ctrl + I toggle info
                if (e.KeyModifiers == KeyModifiers.Control && DataContext is MainViewModel mvm)
                {
                    mvm.CurrentLayout.CodexInfoVM.ShowCodexInfo = !((MainViewModel)DataContext).CurrentLayout.CodexInfoVM.ShowCodexInfo;
                    e.Handled = true;
                }
                break;

            case Key.F5:
                await MainViewModel.CollectionVM.Refresh();
                e.Handled = true;
                break;
        }

    }

    private async void CollectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //only refresh if a new item is set
        if (e.AddedItems.Count > 0)
        {
            await MainViewModel.CollectionVM.Refresh();
        }
    }
    
    private void Toggle_ContextMenu(object sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.ContextMenu != null)
        {
            if (control.ContextMenu.IsOpen)
            {
                control.ContextMenu.Close();
            }
            else
            {
                control.ContextMenu.PlacementTarget = control;
                control.ContextMenu.Open();
            }
        }
    }

    private void LayoutSelection_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && 
            cb.DataContext is MainViewModel vm &&
            e.AddedItems.Count > 0 &&
            e.AddedItems[0] is LayoutViewModel layoutVm)
        {
            vm.ChangeLayoutCommand.Execute(layoutVm.LayoutType);
        }
    }
}
