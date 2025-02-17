﻿using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;

namespace COMPASS.Common.ViewModels.Layouts
{
    public class TileLayoutViewModel : LayoutViewModel
    {
        public TileLayoutViewModel() : base()
        {
            LayoutType = Layout.Tile;
            Preferences = PreferencesService.GetInstance().Preferences.TileLayoutPreferences;
        }

        //TODO check if this is still needed
        //public override bool DoVirtualization => Properties.Settings.Default.DoVirtualizationTile
        //                                         && MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdTile;

        public TileLayoutPreferences Preferences { get; set; }
    }
}
