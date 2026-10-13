using COMPASS.Common.Features.Codices;
using System.ComponentModel;

namespace COMPASS.Common.Features.Filtering
{
    public interface IFilterService
    {
        IList<CodexViewModel> FilterCodices(IList<CodexViewModel> codexVms, FiltersState filtersState);
        void SortCodices(IList<CodexViewModel> codexVms, string sortProperty, ListSortDirection sortDirection);
    }
}
