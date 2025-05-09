using System.Threading.Tasks;

namespace COMPASS.Common.Interfaces.Storage;

public interface IApplicationDataService
{
    /// <summary>
    /// Updates the base data path
    /// </summary>
    /// <param name="newPath"></param>
    /// <returns></returns>
    Task<bool> UpdateRootDirectory(string newPath);

    /// <summary>
    /// If the existing root directory is inaccessible for any reason, prompt the user to pick another one
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    Task RequireNewCompassDataLocation(string msg);
}