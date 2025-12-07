using COMPASS.Common.Interfaces.Services;

namespace COMPASS.Tests.Common.Mocks;

public class MockEnvironmentVarsService : IEnvironmentVarsService
{
    public string CompassDataPath { get; set; } = @"C:\Users\pauld\AppData\Roaming\COMPASS_TESTDATA";
}