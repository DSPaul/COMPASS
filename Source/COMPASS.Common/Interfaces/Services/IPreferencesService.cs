using COMPASS.Common.Models.Preferences;

namespace COMPASS.Common.Interfaces.Services
{
    public interface IPreferencesService
    {
        Preferences Preferences { get; }
        void SavePreferences();
        Preferences? LoadPreferences();
    }
}
