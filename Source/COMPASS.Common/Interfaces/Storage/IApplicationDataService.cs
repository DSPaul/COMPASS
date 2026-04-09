using COMPASS.Common.Models;

namespace COMPASS.Common.Interfaces.Storage;

public interface IApplicationDataService
{
    /// <summary>
    /// Fixed, machine-local directory for app-managed data: redirect file, logs, thumbnails, installers.
    /// Never redirected and never synced.
    /// </summary>
    public static readonly string ApplicationDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.DIR_ROOT);

    string UserDataPath { get; set; }

    /// <summary>
    /// One-time migration from v1 data location (roaming with env variable) to v2 location (local with redirect file).
    /// </summary>
    void MigrateFromV1();

    /// <summary>
    /// Updates the base data path
    /// </summary>
    /// <param name="newPath"></param>
    /// <returns></returns>
    Task<bool> UpdateUserDataPath(string newPath);

    /// <summary>
    /// Updates the base data path
    /// </summary>
    /// <param name="newPath"></param>
    /// <returns></returns>
    Task<bool> ResetUserDataPath();

    /// <summary>
    /// If the existing root directory is inaccessible for any reason, prompt the user to pick another one
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    Task RequireNewUserDataLocation(string msg);
}
