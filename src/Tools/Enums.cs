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
            File = 2,
            Folder = 4,
            GmBinder = 8,
            Homebrewery = 16,
            DnDBeyond = 32,
            GoogleDrive = 64,
            ISBN = 128
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
