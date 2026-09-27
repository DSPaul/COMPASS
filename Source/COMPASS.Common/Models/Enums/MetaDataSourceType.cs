using System.ComponentModel.DataAnnotations;

namespace COMPASS.Common.Models.Enums;

[Flags]
public enum MetadataSourceType
{
    [Display(Name = "None")] None = 0,
    [Display(Name = "File Name/Path")] File = 1,
    [Display(Name = "PDF File")] PDF = 2,
    [Display(Name = "Image File")] Image = 4,
    [Display(Name = "GM Binder")] GmBinder = 8,
    [Display(Name = "Homebrewery")] Homebrewery = 16,
    [Display(Name = "Dnd Beyond")] DnDBeyond = 32,
    [Display(Name = "Google Drive")] GoogleDrive = 64,
    [Display(Name = "Dropbox")] Dropbox = 128,
    [Display(Name = "Open Library (ISBN)")] ISBN = 256,
    [Display(Name = "Website Header")] GenericURL = 512,
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