using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Controls;
using COMPASS.Common.Models;
using COMPASS.Common.Operations;

namespace COMPASS.Common.Views.Layouts;

public partial class TileLayout : CodexLayoutView
{
    public TileLayout()
    {
        InitializeComponent();
    }

    private void ListBox_KeyDown(object? sender, KeyEventArgs e) => CodexOperations.HandleKeyDownOnCodex((sender as ListBox)?.SelectedItems, e);

    private void Codex_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Grid container && container.DataContext is Codex codex)
        {
            CodexOperations.OpenCodex(codex);
        }
    }
}