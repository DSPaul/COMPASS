using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using COMPASS.Infra.ExtensionMethods;
using static COMPASS.Infra.Tools.VisualTreeHelpers;

namespace COMPASS.Infra.Models.DragDrop;

/// <summary>
/// Drag handler that creates a <see cref="ReorderPayload"/> from the dragged visual's
/// position in an <see cref="IList"/>-backed <see cref="ItemsControl"/>.
/// </summary>
public class ReorderDragHandler : DragHandler
{
    public override void TryAddToTransfer(DataTransfer transfer, Visual source)
    {
        var containingItemsControl = FindContainingItemsControl(source);
        if (containingItemsControl?.ItemsSource is not IList itemsList) return;

        var item = (source as Control)?.DataContext;
        if (item is null) return;

        ItemsControl sourceRoot = source.FindAncestorOfType<TreeView>(includeSelf: true) ?? containingItemsControl;

        transfer.AddData(ReorderPayload.Format, new ReorderPayload(itemsList, item, sourceRoot));
    }
}
