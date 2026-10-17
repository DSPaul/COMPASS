using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Features.Codices;

namespace COMPASS.Common.Features.Layouts.Card;

public partial class CardLayout : CodexLayoutView
{
    public CardLayout()
    {
        InitializeComponent();
    }

    private void ListBox_KeyDown(object? sender, KeyEventArgs e)
    {
        CodexCommands?.HandleKeyDownOnCodex((sender as ListBox)?.SelectedItems, e);
    }

    private async void Codex_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { DataContext: CodexViewModel codexVm })
        {
            if (CodexCommands is { } operations)
            {
                await operations.OpenCodex(codexVm.GetModel());
            }
        }
    }
}