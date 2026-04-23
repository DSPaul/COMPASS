using COMPASS.Common.Models.Filters;
using Avalonia.Media;
using COMPASS.Common.Models.CodexProperties;

namespace COMPASS.Common.ViewModels.ModelVMs;

public static class ModelVmFactory
{
    public static FilterViewModel GetFilterViewModel(Filter filter)
    {
        return filter switch
        {
            AuthorFilter => new(filter, Colors.Orange, $"Author: {filter.FilterValue}"),
            DomainFilter => new(filter, Colors.MediumTurquoise, $"From: {filter.FilterValue}"),
            FavoriteFilter => new(filter, Colors.HotPink, "Favorite"),
            FileExtensionFilter => new(filter, Colors.OrangeRed, $"File Type: {filter.FilterValue}"),
            HasBrokenPathFilter => new(filter,  Colors.Gold, "Has broken path"),
            MinimumRatingFilter => new(filter, Colors.Goldenrod, $"At least {filter.FilterValue} stars"),
            NotEmptyFilter  => new(filter, Colors.LightSlateGray,  $"Has value for '{(filter.FilterValue as CodexProperty)?.Label}'"),
            OfflineSourceFilter => new(filter, Colors.DarkSeaGreen, "Available Offline"),
            OnlineSourceFilter => new(filter, Colors.DarkSeaGreen, "Available Online"),
            PhysicalSourceFilter => new(filter, Colors.DarkSeaGreen, "Physically Owned"),
            PublisherFilter  => new(filter, Colors.DarkSeaGreen, $"Publisher: {filter.FilterValue}"),
            SearchFilter => new(filter, Colors.Salmon, $"Search: {filter.FilterValue}"),
            StartReleaseDateFilter => new(filter, Colors.DeepSkyBlue, $"After: {(filter.FilterValue as DateTime?)?.ToShortDateString()}"),
            StopReleaseDateFilter => new(filter,  Colors.DeepSkyBlue, $"Before: {(filter.FilterValue as DateTime?)?.ToShortDateString()}"),
            TagFilter =>  new(filter, (filter.FilterValue as TagViewModel)!.BackgroundColor, (filter.FilterValue as TagViewModel)!.Name),
            _ => throw new NotImplementedException("No vm is defined for filter of type  " + filter.GetType())
        };
    }
}