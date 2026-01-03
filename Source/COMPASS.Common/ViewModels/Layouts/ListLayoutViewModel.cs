using System.Collections.Specialized;
using Avalonia.Collections;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Common.ViewModels.Layouts
{
    public class ListLayoutViewModel : LayoutViewModel
    {
        public ListLayoutViewModel(CollectionTabVM tabVM) : base(tabVM)
        {
            Preferences = PreferencesService.GetInstance().Preferences.ListLayoutPreferences;
        }

        //TODO check if this is still needed
        //public override bool DoVirtualization =>
        //    Properties.Settings.Default.DoVirtualizationList &&
        //    MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdList;

        public ListLayoutPreferences Preferences { get; }
        
        public override CodexLayout LayoutType => CodexLayout.List;
    }
}
