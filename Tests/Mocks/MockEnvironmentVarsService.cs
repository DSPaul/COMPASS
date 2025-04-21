using COMPASS.Common.Interfaces;

namespace Tests.Mocks;

public class MockEnvironmentVarsService : IEnvironmentVarsService
{
    //TODO, make this a temp path or something
    public string CompassDataPath { get; set; } = @"C:\Users\pauld\AppData\Roaming\COMPASS_TESTDATA";
}