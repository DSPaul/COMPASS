using Avalonia.Input;
using COMPASS.Common.Models.Filters;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.Models.DragDrop
{
    public static class DataTransferFormats
    {
        public static DataFormat<Tag> TagFormat { get; } = DataFormat.CreateInProcessFormat<Tag>(nameof(TagFormat));
        public static Tag? TryGetTag(this IDataTransfer transfer) => transfer.TryGetValue(TagFormat);
        public static void AddTag(this DataTransfer transfer, Tag tag) => transfer.AddData(TagFormat, tag);

        public static DataFormat<Filter> FilterFormat { get; } = DataFormat.CreateInProcessFormat<Filter>(nameof(FilterFormat));
        public static Filter? TryGetFilter(this IDataTransfer transfer) => transfer.TryGetValue(FilterFormat);
        public static void AddFilter(this DataTransfer transfer, Filter filter) => transfer.AddData(FilterFormat, filter);
    }
}
