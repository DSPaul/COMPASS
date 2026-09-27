using COMPASS.Infra.Preferences;

namespace COMPASS.Tests.Common.Mocks;

public class MockPreferencesService : IPreferencesService
{
    public T GetPreferences<T>() where T : IPreferences, new() => new T();

    public void SavePreferences() { }
}
