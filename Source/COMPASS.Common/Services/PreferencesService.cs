using System;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Services
{
    public class PreferencesService
    {
        #region singleton pattern
        private PreferencesService() { }
        private static PreferencesService? _prefService;
        public static PreferencesService GetInstance() => _prefService ??= new PreferencesService();
        #endregion

        public string PreferencesFilePath => Path.Combine(ServiceResolver.Resolve<IEnvironmentVarsService>().CompassDataPath, "Preferences.xml");

        public static readonly Lock _writeLocker = new();

        private Preferences? _preferences;
        public Preferences Preferences => _preferences ??= LoadPreferences() ?? new Preferences();

        public void SavePreferences()
        {
            try
            {
                if (_preferences == null) return; //don't save when they aren't loaded
                PreferencesDto dto = _preferences.ToDto();

                string tempFileName = PreferencesFilePath + ".tmp";

                lock (_writeLocker)
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
                    return prefsDto.ToModel();
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
