using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Exceptions;
using COMPASS.Common.Models;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Common.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        /// <summary>
        /// Shortcut because we need this all over the place
        /// </summary>
        public CodexCollection ActiveCollection
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