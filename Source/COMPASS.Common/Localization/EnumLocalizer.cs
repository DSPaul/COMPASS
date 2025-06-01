using System;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.Localization;

public static class EnumLocalizer
{
    public static string Localize(Enum? value) => value switch
    {
        MetaDataSourceType source => FromMetaDataSource(source),
        _ => throw new NotImplementedException(),
    };

    private static string FromMetaDataSource(MetaDataSourceType sourceType) => sourceType switch
    {
        MetaDataSourceType.None => "None",
        MetaDataSourceType.File => "File Name/Path",
        MetaDataSourceType.PDF => "PDF File",
        MetaDataSourceType.Image => "Image File",
        MetaDataSourceType.GmBinder => "GM Binder",
        MetaDataSourceType.Homebrewery => "Homebrewery",
        MetaDataSourceType.GoogleDrive => "Google Drive",
        MetaDataSourceType.ISBN => "Open Library (ISBN)",
        MetaDataSourceType.GenericURL => "Website Header",
        MetaDataSourceType.Dropbox => "Dropbox",
        MetaDataSourceType.DnDBeyond => "Dnd Beyond",
        _ => throw new NotImplementedException(),
    };
}