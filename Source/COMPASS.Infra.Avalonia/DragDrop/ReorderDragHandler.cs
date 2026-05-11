using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using COMPASS.Infra.Avalonia.ExtensionMethods;

namespace COMPASS.Infra.Avalonia.DragDrop;

/// <summary>
/// Drag handler that creates a <see cref="ReorderPayload"/> from the dragged visual's
/// position in an <see cref="IList"/>-backed <see cref="ItemsControl"/>.
/// </summary>
public class ReorderDragHandler : DragHandler
{
    public override void TryAddToTransfer(DataTransfer transfer, Visual source)
    {
        var containingItemsControl = source.FindContainingItemsControl();
        if (containingItemsControl?.ItemsSource is not IList itemsList) return;

        var item = (source as Control)?.DataContext;
        if (item is null) return;

        ItemsControl sourceRoot = source.FindAncestorOfType<TreeView>(includeSelf: true) ?? containingItemsControl;

        transfer.AddData(ReorderPayload.Format, new ReorderPayload(itemsList, item, sourceRoot));
    }
}
