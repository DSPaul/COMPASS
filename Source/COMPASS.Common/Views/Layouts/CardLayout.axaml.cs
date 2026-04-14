using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Operations;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.Views.Layouts;

public partial class CardLayout : CodexLayoutView
{
    public CardLayout()
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
        if (sender is Border { DataContext: CodexViewModel codexVm })
        {
            await CodexOperations.OpenCodex(codexVm.GetModel());
        }
    }

    private void Codex_DragOver(object? sender, DragEventArgs e) => CodexOperations.OnDragOver(sender, e);
    private void Codex_Drop(object? sender, DragEventArgs e) => CodexOperations.OnDrop(sender, e);
    private void Codex_DragEnter(object? sender, DragEventArgs e) => CodexOperations.OnDragEnter(sender, e);
    private void Codex_DragLeave(object? sender, DragEventArgs e) => CodexOperations.OnDragLeave(sender, e);
}