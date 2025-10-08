using System.Collections.Generic;

namespace COMPASS.Common.Models.Filters;

public class FiltersState
{
    public List<Filter> IncludedFilters { get; set; } = [];
    public List<Filter> ExcludedFilters { get; set; } = [];
}