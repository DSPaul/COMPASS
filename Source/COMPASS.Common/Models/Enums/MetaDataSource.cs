using System;

namespace COMPASS.Common.Models.Enums;

[Flags]
public enum MetaDataSource
{
    None = 0,
    File = 1,
    PDF = 2,
    Image = 4,
    GmBinder = 8,
    Homebrewery = 16,
    DnDBeyond = 32,
    GoogleDrive = 64,
    Dropbox = 128,
    ISBN = 256,
    GenericURL = 512,
}

public static class MetaDataSources
{
    public static readonly MetaDataSource OnlineSources =
        MetaDataSource.GmBinder |
        MetaDataSource.Homebrewery |
        MetaDataSource.DnDBeyond |
        MetaDataSource.GoogleDrive |
        MetaDataSource.Dropbox |
        MetaDataSource.GenericURL;

    public static readonly MetaDataSource OfflineSources =
        MetaDataSource.File |
        MetaDataSource.PDF |
        MetaDataSource.Image;
}