using System.Xml;
using System.Xml.Serialization;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Services.FileSystem;

namespace COMPASS.Common.Services
{
    public class PreferencesService(ILogger logger, IApplicationDataService applicationDataService) : IPreferencesService
    {
        public string PreferencesFilePath => Path.Combine(applicationDataService.UserDataPath, "Preferences.xml");

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
                    //cleanup any previous temp file
                    File.Delete(tempFileName);

                    //Write to the temp file
                    using (var writer = XmlWriter.Create(tempFileName, XmlService.XmlWriteSettings))
                    {
                        XmlSerializer serializer = new(typeof(PreferencesDto));
                        serializer.Serialize(writer, dto);
                    }

                    // Verify the temp file was written successfully and has content
                    if (!File.Exists(tempFileName) || new FileInfo(tempFileName).Length <= 0)
                    {
                        logger.Error($"Failed to write preferences to {tempFileName}", new Exception());
                        return;
                    }

                    //if successfully written to the tmp file, move to actual path
                    File.Move(tempFileName, PreferencesFilePath, true);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error($"Access denied when trying to save Preferences to {PreferencesFilePath}", ex);
            }
            catch (IOException ex)
            {
                logger.Error($"IO error occurred when saving Preferences to {PreferencesFilePath}", ex);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to save Preferences to {PreferencesFilePath}", ex);
            }
        }

        public Preferences? LoadPreferences()
        {
            if (!File.Exists(PreferencesFilePath))
            {
                logger.Debug($"{PreferencesFilePath} does not exist.");
                return null;
            }

            try
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
                
                logger.Error($"{PreferencesFilePath} could not be read.", new Exception());
            }
            catch (XmlException ex)
            {
                logger.Error($"XML parsing error in {PreferencesFilePath}. File may be corrupted or empty.", ex);
            }
            catch (InvalidOperationException ex) when (ex.InnerException is XmlException)
            {
                logger.Error($"XML deserialization error in {PreferencesFilePath}. File may be corrupted or empty.", ex);
            }
            catch (Exception ex)
            {
                logger.Error($"Unexpected error loading preferences from {PreferencesFilePath}", ex);
            }

            return null;
        }
    }
}
