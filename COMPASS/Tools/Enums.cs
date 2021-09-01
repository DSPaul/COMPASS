using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.Tools
{
    public class Enums
    {
        public enum FileView
        {
            ListView,
            CardView,
            TileView
        }

        public enum ImportMode
        {
            Pdf,
            Manual,
            GmBinder,
            Homebrewery
        }

        public enum FilterType
        {
            Author,
            Publisher,
            StartReleaseDate,
            StopReleaseDate,
            MinimumRating
        }
    }
}
