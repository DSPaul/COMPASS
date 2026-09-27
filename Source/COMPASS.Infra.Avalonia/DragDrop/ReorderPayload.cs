using Avalonia.Controls;
using Avalonia.Input;
using System.Collections;

namespace COMPASS.Infra.Avalonia.DragDrop;

/// <summary>
/// Payload carried during a reorder drag operation.
/// </summary>
public sealed record ReorderPayload(IList SourceList, object DraggedItem, ItemsControl SourceRoot)
{
    public static DataFormat<ReorderPayload> Format { get; } =
        DataFormat.CreateInProcessFormat<ReorderPayload>("ReorderPayload");
}
