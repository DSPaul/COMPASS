using System;
using Avalonia.Data.Converters;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.Converters;

public static class EnumLocalizers
{
    public static FuncValueConverter<MetaDataSource, string> FromMetaDataSource { get; } = new(source => source switch
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
    });
}