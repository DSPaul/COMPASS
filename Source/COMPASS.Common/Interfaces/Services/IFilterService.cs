using COMPASS.Common.Models.Filters;
using COMPASS.Common.ViewModels.ModelVMs;
using System.ComponentModel;

namespace COMPASS.Common.Interfaces.Services
{
    public interface IFilterService
    {
        IList<CodexViewModel> FilterCodices(IList<CodexViewModel> codexVms, FiltersState filtersState);
        void SortCodices(IList<CodexViewModel> codexVms, string sortProperty, ListSortDirection sortDirection);
    }
}
