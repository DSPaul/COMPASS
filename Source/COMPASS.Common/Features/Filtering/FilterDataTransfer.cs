using Avalonia.Input;
using COMPASS.Infra.Avalonia.DragDrop;

namespace COMPASS.Common.Features.Filtering;

public static class FilterDataTransfer
{
    public static DataFormat<Filter> FilterFormat { get; } = DataFormat.CreateInProcessFormat<Filter>(nameof(FilterFormat));
    public static Filter? TryGetFilter(this IDataTransfer transfer) => transfer.TryGetValue(FilterFormat);
    public static void AddFilter(this DataTransfer transfer, Filter filter) => transfer.AddData(FilterFormat, filter);
}
