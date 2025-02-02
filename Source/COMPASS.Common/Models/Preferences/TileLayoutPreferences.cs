using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Common.Models.Preferences
{
    public class TileLayoutPreferences : ObservableObject
    {
        public enum DataOption
        {
            Title,
            Author,
            Publisher,
            Rating
        }

        private double _tileWidth;
        public double TileWidth
        {
            get => _tileWidth;
            set
            {
                SetProperty(ref _tileWidth, value);
                OnPropertyChanged(nameof(TileHeight));
            }
        }

        public double TileHeight => (int)(TileWidth * 4 / 3);

        private bool _showExtraData;
        public bool ShowExtraData
        {
            get => _showExtraData;
            set => SetProperty(ref _showExtraData, value);
        }

        private DataOption _displayedData;
        public DataOption DisplayedData
        {
            get => _displayedData;
            set => SetProperty(ref _displayedData, value);
        }

        public DataOption[] DataOptions { get; } = Enum.GetValues<DataOption>();
    }
}