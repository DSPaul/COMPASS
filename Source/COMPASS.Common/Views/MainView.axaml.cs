using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.ViewModels;

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
                    //Searchbar.Focus();
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
}
