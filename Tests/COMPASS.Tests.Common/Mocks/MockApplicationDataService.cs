using System.Threading.Tasks;
using COMPASS.Common.Interfaces.Storage;

namespace COMPASS.Tests.Common.Mocks;

public class MockApplicationDataService : IApplicationDataService
{
    public string UserDataPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "COMPASS_TESTDATA");

    public Task<bool> UpdateUserDataPath(string newPath) => Task.FromResult(false);
    public Task<bool> ResetUserDataPath() => Task.FromResult(false);

    public Task RequireNewUserDataLocation(string msg) => Task.CompletedTask;

    public void MigrateFromV1() { }

}
