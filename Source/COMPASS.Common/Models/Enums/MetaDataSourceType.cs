using System;

namespace COMPASS.Common.Models.Enums;

[Flags]
public enum MetadataSourceType
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

public static class MetadataSources
{
    public static readonly MetadataSourceType OnlineSources =
        MetadataSourceType.GmBinder |
        MetadataSourceType.Homebrewery |
        MetadataSourceType.DnDBeyond |
        MetadataSourceType.GoogleDrive |
        MetadataSourceType.Dropbox |
        MetadataSourceType.GenericURL;

    public static readonly MetadataSourceType OfflineSources =
        MetadataSourceType.File |
        MetadataSourceType.PDF |
        MetadataSourceType.Image;
}