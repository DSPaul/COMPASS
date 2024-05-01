using COMPASS.Common.Models;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Tools;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace COMPASS.Common.Services
{
    public class PreferencesService
    {
        #region singleton pattern
        private PreferencesService() { }
        private static PreferencesService? _prefService;
        public static PreferencesService GetInstance() => _prefService ??= new PreferencesService();
        #endregion

        public string PreferencesFilePath => Path.Combine(EnvironmentVarsService.CompassDataPath, "Preferences.xml");

        private Preferences? _preferences;
        public Preferences Preferences => _preferences ??= LoadPreferences() ?? new Preferences();

        public void SavePreferences()
        {
            if (_preferences == null) return; //don't save when they aren't loaded
            PreferencesDto dto = _preferences.ToDto();
            using var writer = XmlWriter.Create(PreferencesFilePath, XmlService.XmlWriteSettings);
            XmlSerializer serializer = new(typeof(PreferencesDto));
            serializer.Serialize(writer, dto);
        }

        public Preferences? LoadPreferences()
        {
            if (File.Exists(PreferencesFilePath))
            {
                //Label of codexProperties should still be deserialized for backwards compatibility
                var overrides = new XmlAttributeOverrides();
                var overwriteIgnore = new XmlAttributes { XmlIgnore = false };
                overrides.Add(typeof(CodexProperty), nameof(CodexProperty.Label), overwriteIgnore);

                using var reader = new StreamReader(PreferencesFilePath);
                XmlSerializer serializer = new(typeof(PreferencesDto), overrides);
                if (serializer.Deserialize(reader) is PreferencesDto prefsDto)
                {
                    return prefsDto is null ? new() : new(prefsDto);
                }
                else
                {
                    Logger.Error($"{PreferencesFilePath} could not be read.", new Exception());
                    return null;
                }
            }
            else
            {
                Logger.Warn($"{PreferencesFilePath} does not exist.", new FileNotFoundException());
                return null;
            }
        }

    }
}
