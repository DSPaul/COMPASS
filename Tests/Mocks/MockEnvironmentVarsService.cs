using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Services;

namespace Tests.Mocks;

public class MockEnvironmentVarsService : IEnvironmentVarsService
{
    //TODO, make this a temp path or something
    public string CompassDataPath { get; set; } = @"C:\Users\pauld\AppData\Roaming\COMPASS_TESTDATA";
}