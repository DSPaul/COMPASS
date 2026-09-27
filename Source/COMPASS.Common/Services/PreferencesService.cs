using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Preferences;
using COMPASS.Infra.Updates;
using COMPASS.Infra.Xml;
using System.Xml;
using System.Xml.Serialization;

namespace COMPASS.Common.Services;

public class PreferencesService : IPreferencesService
{
    private ILogger _logger;
    private IApplicationDataService _applicationDataService;

    public PreferencesService(ILogger logger, IApplicationDataService applicationDataService)
    {
        _logger = logger;
        _applicationDataService = applicationDataService;

        _preferences = new Lazy<Preferences>(() => LoadPreferences() ?? new Preferences());
    }

    public string PreferencesFilePath => Path.Combine(_applicationDataService.UserDataPath, "Preferences.xml");

    public readonly Lock _fileLock = new();

    private static readonly Dictionary<Type, Func<Preferences, object>> _sections = new()
    {
        [typeof(UpdatePreferences)] = p => p.UpdatePreferences,
        [typeof(UIState)] = p => p.UIState,
        [typeof(WindowRestoreState)] = p => p.WindowState,
        [typeof(ListLayoutPreferences)] = p => p.ListLayoutPreferences,
        [typeof(CardLayoutPreferences)] = p => p.CardLayoutPreferences,
        [typeof(TileLayoutPreferences)] = p => p.TileLayoutPreferences,
        [typeof(HomeLayoutPreferences)] = p => p.HomeLayoutPreferences,
        [typeof(Preferences)] = p => p,
    };

    public T GetPreferences<T>() where T : IPreferences, new()
    {
        if (_sections.TryGetValue(typeof(T), out var accessor))
            return (T)accessor(_preferences.Value);

        throw new NotSupportedException(
            $"No preferences section registered for {typeof(T).Name}. " +
            $"Add it to {nameof(_sections)} in {nameof(PreferencesService)}.");
    }

    public void SavePreferences()
    {
        try
        {
            if (!_preferences.IsValueCreated) return; //don't save when they aren't loaded
            lock (_fileLock)
            {
                PreferencesDto dto = _preferences.Value.ToDto();

                string tempFileName = PreferencesFilePath + ".tmp";

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
                    _logger.Error($"Failed to write preferences to {tempFileName}", new Exception());
                    return;
                }

                //if successfully written to the tmp file, move to actual path
                File.Move(tempFileName, PreferencesFilePath, true);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Error($"Access denied when trying to save Preferences to {PreferencesFilePath}", ex);
        }
        catch (IOException ex)
        {
            _logger.Error($"IO error occurred when saving Preferences to {PreferencesFilePath}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save Preferences to {PreferencesFilePath}", ex);
        }
    }

    private Lazy<Preferences> _preferences;
    private Preferences? LoadPreferences()
    {
        string filePath = PreferencesFilePath;

        if (!File.Exists(filePath))
        {
            _logger.Debug($"{filePath} does not exist.");
            return null;
        }

        try
        {
            lock(_fileLock)
            {
                //Label of codexProperties should still be deserialized for backwards compatibility
                var overrides = new XmlAttributeOverrides();
                var overwriteIgnore = new XmlAttributes { XmlIgnore = false };
                overrides.Add(typeof(CodexProperty), nameof(CodexProperty.Label), overwriteIgnore);

                using var reader = new StreamReader(filePath);
                XmlSerializer serializer = new(typeof(PreferencesDto), overrides);
                if (serializer.Deserialize(reader) is PreferencesDto prefsDto)
                {
                    return prefsDto.ToModel();
                }
            
                _logger.Error($"{filePath} could not be read.", new Exception());
            }
        }
        catch (XmlException ex)
        {
            _logger.Error($"XML parsing error in {filePath}. File may be corrupted or empty.", ex);
        }
        catch (InvalidOperationException ex) when (ex.InnerException is XmlException)
        {
            _logger.Error($"XML deserialization error in {filePath}. File may be corrupted or empty.", ex);
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error loading preferences from {filePath}", ex);
        }

        return null;
    }
}
