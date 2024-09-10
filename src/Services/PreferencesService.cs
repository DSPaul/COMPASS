using COMPASS.Models.CodexProperties;
using COMPASS.Models.Preferences;
using COMPASS.Models.XmlDtos;
using COMPASS.Tools;
using COMPASS.ViewModels;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace COMPASS.Services
{
    public class PreferencesService
    {
        #region singleton pattern
        private PreferencesService() { }
        private static PreferencesService? _prefService;
        public static PreferencesService GetInstance() => _prefService ??= new PreferencesService();
        #endregion

        public string PreferencesFilePath => Path.Combine(SettingsViewModel.CompassDataPath, "Preferences.xml");

        public static object writeLocker = new();

        private Preferences? _preferences;
        public Preferences Preferences => _preferences ??= LoadPreferences() ?? new Preferences();

        public void SavePreferences()
        {
            Properties.Settings.Default.Save();

            if (_preferences == null) return; //don't save when they aren't loaded
            PreferencesDto dto = _preferences.ToDto();

            try
            {
                string tempFileName = PreferencesFilePath + ".tmp";

                lock (writeLocker)
                {
                    using (var writer = XmlWriter.Create(tempFileName, XmlService.XmlWriteSettings))
                    {
                        XmlSerializer serializer = new(typeof(PreferencesDto));
                        serializer.Serialize(writer, dto);
                    }

                    //if successfully written to the tmp file, move to actual path
                    File.Move(tempFileName, PreferencesFilePath, true);
                    File.Delete(tempFileName);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"Access denied when trying to save Preferences to {PreferencesFilePath}", ex);
            }
            catch (IOException ex)
            {
                Logger.Error($"IO error occurred when saving Preferences to {PreferencesFilePath}", ex);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save Preferences to {PreferencesFilePath}", ex);
            }
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
                    return prefsDto is null ? new() : prefsDto.ToModel();
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
