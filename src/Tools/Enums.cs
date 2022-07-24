using System;

namespace COMPASS.Tools
{
    public static class Enums
    {
        public enum CodexLayout
        {
            ListLayout,
            CardLayout,
            TileLayout,
            HomeLayout
        }

        [Flags]
        public enum Sources
        {
            None = 0,
            Manual = 1,
            Pdf = 2,
            GmBinder = 4,
            Homebrewery = 8,
            DnDBeyond = 16,
            GoogleDrive = 32,
            ISBN = 64
        }

        public enum FilterType
        {
            Search,
            Author,
            Publisher,
            StartReleaseDate,
            StopReleaseDate,
            MinimumRating,
            OnlineSource,
            OfflineSource,
            PhysicalSource
        }

        public enum Browser
        {
            Chrome,
            Firefox,
            Edge
        }
    }
}
