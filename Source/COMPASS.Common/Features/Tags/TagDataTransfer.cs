using Avalonia.Input;
using COMPASS.Infra.Avalonia.DragDrop;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Tags;

public static class TagDataTransfer
{
    public static DataFormat<Tag> TagFormat { get; } = DataFormat.CreateInProcessFormat<Tag>(nameof(TagFormat));
    public static Tag? TryGetTag(this IDataTransfer transfer) => transfer.TryGetValue(TagFormat);
    public static void AddTag(this DataTransfer transfer, Tag tag) => transfer.AddData(TagFormat, tag);
}
