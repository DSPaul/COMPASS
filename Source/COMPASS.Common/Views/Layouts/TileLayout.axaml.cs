using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Operations;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.Views.Layouts;

public partial class TileLayout : CodexLayoutView
{
    public TileLayout()
    {
        InitializeComponent();
    }

    private void ListBox_KeyDown(object? sender, KeyEventArgs e)
    {
        var operations = TabsViewModel.GetInstance().ActiveTab!.CodexCommands;
        operations.HandleKeyDownOnCodex((sender as ListBox)?.SelectedItems, e);
    }

    private async void Codex_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Grid { DataContext: CodexViewModel codexVm })
        {
            await CodexOperations.OpenCodex(codexVm.GetModel());
        }
    }
}