using System;

namespace COMPASS.Common.Models.Enums;

[Flags]
public enum MetaDataSourceType
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
    public static readonly MetaDataSourceType OnlineSources =
        MetaDataSourceType.GmBinder |
        MetaDataSourceType.Homebrewery |
        MetaDataSourceType.DnDBeyond |
        MetaDataSourceType.GoogleDrive |
        MetaDataSourceType.Dropbox |
        MetaDataSourceType.GenericURL;

    public static readonly MetaDataSourceType OfflineSources =
        MetaDataSourceType.File |
        MetaDataSourceType.PDF |
        MetaDataSourceType.Image;
}