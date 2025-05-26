using System;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.Localization;

public static class EnumLocalizer
{
    public static string Localize(Enum? value) => value switch
    {
        MetaDataSource source => FromMetaDataSource(source),
        _ => throw new NotImplementedException(),
    };

    private static string FromMetaDataSource(MetaDataSource source) => source switch
    {
        MetaDataSource.None => "None",
        MetaDataSource.File => "File Name/Path",
        MetaDataSource.PDF => "PDF File",
        MetaDataSource.Image => "Image File",
        MetaDataSource.GmBinder => "GM Binder",
        MetaDataSource.Homebrewery => "Homebrewery",
        MetaDataSource.GoogleDrive => "Google Drive",
        MetaDataSource.ISBN => "Open Library (ISBN)",
        MetaDataSource.GenericURL => "Website Header",
        MetaDataSource.Dropbox => "Dropbox",
        MetaDataSource.DnDBeyond => "Dnd Beyond",
        _ => throw new NotImplementedException(),
    };
}