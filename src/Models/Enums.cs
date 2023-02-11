using System;

namespace COMPASS.Models
{
    public static class Enums
    {
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

        public enum Browser
        {
            Chrome,
            Firefox,
            Edge
        }
    }
}
