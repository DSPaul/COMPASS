namespace COMPASS.Common.Features.Filtering;

public class FiltersState
{
    public List<Filter> IncludedFilters { get; set; } = [];
    public List<Filter> ExcludedFilters { get; set; } = [];
}