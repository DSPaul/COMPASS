using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Operations;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.Views.Layouts;

public partial class HomeLayout : CodexLayoutView
{
    public HomeLayout()
    {
        InitializeComponent();
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        var operations = TabsViewModel.GetInstance().ActiveTab!.CodexCommands;
        operations.HandleKeyDownOnCodex((sender as ListBox)?.SelectedItems, e);
    }

    private async void Codex_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Grid { DataContext: CodexViewModel codexVm })
        {
            var operations = TabsViewModel.GetInstance().ActiveTab!.CodexCommands;
            await operations.OpenCodex(codexVm.GetModel());
        }
    }
}