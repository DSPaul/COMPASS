using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Features.Codices;

namespace COMPASS.Common.Features.Layouts.Home;

public partial class HomeLayout : CodexLayoutView
{
    public HomeLayout()
    {
        InitializeComponent();
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        CodexCommands?.HandleKeyDownOnCodex((sender as ListBox)?.SelectedItems, e);
    }

    private async void Codex_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Grid { DataContext: CodexViewModel codexVm })
        {
            if (CodexCommands is { } operations)
            {
                await operations.OpenCodex(codexVm.GetModel());
            }
        }
    }
}