using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.Tools
{
    public static class Enums
    {
        public enum FileView
        {
            ListView,
            CardView,
            TileView
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
            GoogleDrive = 32
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
