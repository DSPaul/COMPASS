namespace COMPASS.Common.Features.Persistence;

public enum StorageStrategy
{
    /// <summary>
    /// The data is stored in XML files on disk.
    /// </summary>
    Xml,

    /// <summary>
    /// The data is stored in memory only
    /// </summary>
    Memory
}