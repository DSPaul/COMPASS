using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Controls;
using COMPASS.Common.Models;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Layouts;

public partial class TileLayout : CodexLayout
{
    public TileLayout()
    {
        InitializeComponent();
    }

    private void ListBox_KeyDown(object? sender, KeyEventArgs e) => CodexViewModel.ListBoxHandleKeyDown(sender, e);

    private void Codex_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Grid container && container.DataContext is Codex codex)
        {
            CodexViewModel.OpenCodex(codex);
        }
    }
}