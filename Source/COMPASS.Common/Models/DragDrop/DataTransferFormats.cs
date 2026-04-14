using Avalonia.Input;
using COMPASS.Common.Models.Filters;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.Models.DragDrop
{
    public static class DataTransferFormats
    {
        public static DataFormat<string> TagFormat { get; } = DataFormat.CreateStringApplicationFormat(nameof(TagFormat));
        public static Tag? TryGetTag(this IDataTransfer transfer, ICollection<Tag> allTags)
        {
            var dto = transfer.GetValue<TagDto>(TagFormat);
            return allTags?.FirstOrDefault(t => t.Id == dto?.ID);
        }

        public static void AddTag(this DataTransfer transfer, Tag tag)
        {
            var tagDto = new TagDto()
            {
                ID = tag.Id,
                Content = tag.Name,
                BackgroundColor = tag.BackgroundColor,
                IsGroup = tag.IsGroup
            };

            transfer.AddData(TagFormat, tagDto);
        }

        public static DataFormat<string> FilterFormat { get; } = DataFormat.CreateStringApplicationFormat(nameof(FilterFormat));
        public static Filter? TryGetFilter(this IDataTransfer transfer) => transfer.GetValue<Filter>(FilterFormat);
    }
}
