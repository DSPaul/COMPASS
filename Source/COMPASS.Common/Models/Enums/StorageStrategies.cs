namespace COMPASS.Common.Models.Enums;

public enum StorageStrategy
{
    /// <summary>
    /// The data is stored in XML files on disk.
    /// </summary>
    Xml,

    /// <summary>
    /// The data is stored in memory only and not persisted.
    /// </summary>
    Memory
}