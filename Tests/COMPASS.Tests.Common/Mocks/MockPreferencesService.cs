using COMPASS.Infra.Preferences;

namespace COMPASS.Tests.Common.Mocks;

public class MockPreferencesService : IPreferencesService
{
    private readonly Dictionary<Type, IPreferences> _preferencesByType = [];

    /// <summary>
    /// Returns the same instance for every call with the same type, so changes made by the code under test persist
    /// </summary>
    public T GetPreferences<T>() where T : IPreferences, new()
    {
        if (!_preferencesByType.TryGetValue(typeof(T), out IPreferences? preferences))
        {
            preferences = new T();
            _preferencesByType[typeof(T)] = preferences;
        }
        return (T)preferences;
    }

    public void SavePreferences() { }
}
