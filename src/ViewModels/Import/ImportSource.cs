using System;

namespace COMPASS.ViewModels.Import
{
    [Flags]
    public enum ImportSource
    {
        None = 0,
        File = 1,
        Folder = 2,
        Manual = 4,
        GmBinder = 8,
        Homebrewery = 16,
        GoogleDrive = 32,
        ISBN = 64,
        GenericURL = 128,
    }
}
