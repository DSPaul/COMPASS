using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models.Preferences;

namespace COMPASS.Tests.Common.Mocks;

public class MockPreferencesService : IPreferencesService
{
    public Preferences Preferences { get; private set; } = new();

    public void SavePreferences() { }

    public Preferences? LoadPreferences() => Preferences;
}
