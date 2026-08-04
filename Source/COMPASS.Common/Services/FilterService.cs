using Avalonia.Collections;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.Avalonia.ExtensionMethods;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Models;
using System.ComponentModel;

namespace COMPASS.Common.Services
{
    public class FilterService : IFilterService
    {
        public IList<CodexViewModel> FilterCodices(IList<CodexViewModel> codexVms, FiltersState filtersState)
        {
            HashSet<CodexViewModel> includedCodices = GetIncludedCodices(codexVms, filtersState.IncludedFilters);
            HashSet<CodexViewModel> excludedCodices=  GetExcludedCodices(codexVms, filtersState.ExcludedFilters);
            return includedCodices.Except(excludedCodices).ToList();
        }

        public void SortCodices(IList<CodexViewModel> codexVms, string sortProperty, ListSortDirection sortDirection)
        {
            Func<CodexViewModel, object?> keySelector = c => c.GetPropertyValue(sortProperty);

            switch (codexVms)
            {
                case RangeObservableCollection<CodexViewModel> roc:
                    roc.Sort(keySelector, sortDirection);
                    break;
                case AvaloniaList<CodexViewModel> roc:
                    roc.Sort(keySelector, sortDirection);
                    break;
                default:
                    List<CodexViewModel> sorted = sortDirection switch
                    {
                        ListSortDirection.Ascending => codexVms.OrderBy(keySelector).ToList(),
                        ListSortDirection.Descending => codexVms.OrderByDescending(keySelector).ToList(),
                        _ => throw new ArgumentOutOfRangeException(nameof(sortDirection), sortDirection, null)
                    };

                    codexVms.Clear();
                    foreach(var item in sorted)
                    {
                        codexVms.Add(item);
                    }
                    break;
            }
        }

        private HashSet<CodexViewModel> GetIncludedCodices(IList<CodexViewModel> allCodexVms, IEnumerable<Filter> includeFilters)
        {
            var includedCodices = new HashSet<CodexViewModel>(allCodexVms);
            foreach (FilterType filterType in Enum.GetValues(typeof(FilterType)))
            {
                var filteredCodicesByType = FilterCodicesByType(allCodexVms, includeFilters, filterType, true);
                // Included codices must match filters of all types so IntersectWith()
                includedCodices.IntersectWith(filteredCodicesByType);
            }
            return includedCodices;
        }

        private HashSet<CodexViewModel> GetExcludedCodices(IList<CodexViewModel> allCodexVms, IEnumerable<Filter> excludeFilters)
        {
            var excludedCodices = new HashSet<CodexViewModel>();
            foreach (FilterType filterType in Enum.GetValues(typeof(FilterType)))
            {
                var filteredCodicesByType = FilterCodicesByType(allCodexVms, excludeFilters, filterType, false);
                // Codex is excluded as soon as it matches any excluded filter so UnionWith()
                excludedCodices.UnionWith(filteredCodicesByType);
            }
            return excludedCodices;
        }

        /// <summary>
        /// Get list of Codices that match filters of one filter type
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="filterType"></param>
        /// <param name="include"> Determines whether returned codices should be included or excluded </param>
        /// <returns></returns>
        private IEnumerable<CodexViewModel> FilterCodicesByType(IList<CodexViewModel> allCodexVms, IEnumerable<Filter> filters, FilterType filterType, bool include)
        {
            IList<Filter> relevantFilters = filters.Where(filter => filter.Type == filterType).ToList();

            if (relevantFilters.Count == 0) return include ? allCodexVms : Enumerable.Empty<CodexViewModel>();

            return filterType switch
            {
                FilterType.Tag => include ? GetIncludedCodicesByTags(allCodexVms, relevantFilters) : GetExcludedCodicesByTags(allCodexVms, relevantFilters),
                _ => allCodexVms.Where(vm => relevantFilters.Any(filter => filter.Apply(vm.GetModel())))
            };
        }

        private HashSet<CodexViewModel> GetIncludedCodicesByTags(IList<CodexViewModel> allCodexVms, IEnumerable<Filter> filters)
        {
            HashSet<CodexViewModel> includedCodices = [.. allCodexVms];

            List<Tag> includedTags = filters
                .Select(filter => ((TagViewModel)filter.FilterValue!).GetModel())
                .ToList();

            if (includedTags.Count > 0)
            {
                HashSet<Tag> includedGroups = includedTags.Select(tag => tag.GetGroup()).ToHashSet();

                // Go over every group, tags within same group have OR relation, groups have AND relation
                foreach (Tag group in includedGroups)
                {
                    // Make list with all included tags in that group, including children
                    List<Tag> singleGroupTags = includedTags.Where(tag => tag.GetGroup() == group).Flatten().ToList();
                    // Add parents of those tags, must come AFTER children, otherwise children of parents are included which is wrong
                    for (int i = 0; i < singleGroupTags.Count; i++)
                    {
                        Tag? parentTag = singleGroupTags[i].Parent;
                        if (parentTag is not null && !parentTag.IsGroup) singleGroupTags.AddIfMissing(parentTag);
                    }

                    //List of codices that match filters in one group
                    HashSet<CodexViewModel> singleGroupFilteredCodices =
                        [.. allCodexVms.Where(codexVm => singleGroupTags.Intersect(codexVm.GetModel().Tags).Any())];

                    includedCodices = includedCodices.Intersect(singleGroupFilteredCodices).ToHashSet();
                }
            }
            return includedCodices;
        }

        private HashSet<CodexViewModel> GetExcludedCodicesByTags(IList<CodexViewModel> allCodexVms, IEnumerable<Filter> filters)
        {
            HashSet<CodexViewModel> excludedCodices = [];

            var excludedTags = filters.Select(filter => ((TagViewModel)filter.FilterValue!).GetModel()).ToList();

            if (excludedTags.Count > 0)
            {
                // If parent is excluded, so should all the children
                excludedTags = excludedTags.Flatten().ToList();
                excludedCodices = [.. allCodexVms.Where(codexVm => excludedTags.Intersect(codexVm.GetModel().Tags).Any())];
            }

            return excludedCodices;
        }
    }
}
