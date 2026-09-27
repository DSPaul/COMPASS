using COMPASS.Common.Exceptions;
using COMPASS.Common.Models;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Avalonia.Wizard;

namespace COMPASS.Common.ViewModels
{
    public abstract class ViewModelBase : ValidatableViewModelBase
    {
        /// <summary>
        /// Shortcut because we need this all over the place
        /// </summary>
        protected CodexCollection ActiveCollection
        {
            get
            {
                CollectionTabVM tabVm = TabsViewModel.GetInstance().ActiveTab
                                        ?? throw new NoTabException("No collection is active because there is no active tab");
                return tabVm.CollectionVM.Collection;
            }
        }
    }
}
