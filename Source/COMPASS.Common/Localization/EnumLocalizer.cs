using System;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.Localization;

public static class EnumLocalizer
{
    public static string Localize(Enum? value) => value switch
    {
        MetadataSourceType source => FromMetadataSource(source),
        _ => throw new NotImplementedException(),
    };

    private static string FromMetadataSource(MetadataSourceType sourceType) => sourceType switch
    {
        MetadataSourceType.None => "None",
        MetadataSourceType.File => "File Name/Path",
        MetadataSourceType.PDF => "PDF File",
        MetadataSourceType.Image => "Image File",
        MetadataSourceType.GmBinder => "GM Binder",
        MetadataSourceType.Homebrewery => "Homebrewery",
        MetadataSourceType.GoogleDrive => "Google Drive",
        MetadataSourceType.ISBN => "Open Library (ISBN)",
        MetadataSourceType.GenericURL => "Website Header",
        MetadataSourceType.Dropbox => "Dropbox",
        MetadataSourceType.DnDBeyond => "Dnd Beyond",
        _ => throw new NotImplementedException(),
    };
}