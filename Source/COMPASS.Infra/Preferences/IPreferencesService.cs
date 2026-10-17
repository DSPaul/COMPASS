namespace COMPASS.Infra.Preferences;
public interface IPreferencesService
{
    T GetPreferences<T>() where T : IPreferences, new();
    void SavePreferences();
}
