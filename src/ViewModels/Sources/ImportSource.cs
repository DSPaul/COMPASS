using System;

namespace COMPASS.ViewModels.Sources
{
    [Flags]
    public enum ImportSource
    {
        None = 0,
        Manual = 1,
        File = 2,
        Folder = 4,
        GmBinder = 8,
        Homebrewery = 16,
        DnDBeyond = 32,
        GoogleDrive = 64,
        Dropbox = 128,
        ISBN = 256,
        GenericURL = 512
    }
}
