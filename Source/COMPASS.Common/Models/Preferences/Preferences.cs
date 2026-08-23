using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Operations;
using COMPASS.Infra.ExtensionMethods;
using NuGet.Versioning;

namespace COMPASS.Common.Models.Preferences
{
    public class Preferences : ObservableObject
    {
        public Preferences()
        {
            _openCodexPriority = new(OpenCodexFunctions);
            ImportableCodexProperties = SourceMetaData.ImportableProperties.Select(CodexProperty.GetInstance).RemoveNulls().ToList();
            ListLayoutPreferences = new();
            CardLayoutPreferences = new();
            TileLayoutPreferences = new();
            HomeLayoutPreferences = new();
            UIState = new UIState();
            WindowState = new WindowRestoreState();
            UpdatePreferences = new UpdatePreferences();
            AutoLinkFolderTagSameName = true;
        }

        #region Constants
        
        public const int ONLINE_SOURCE_PRIORITY_ID = 0;
        public const int LOCAL_SOURCE_PRIORITY_ID = 1;

        //list with possible functions to open a file
        public static readonly ReadOnlyCollection<PreferableFunction<Codex>> OpenCodexFunctions =
            new List<PreferableFunction<Codex>>()
            {
                new("Online source", CodexOperations.OpenCodexOnline, ONLINE_SOURCE_PRIORITY_ID),
                new("Local File", CodexOperations.OpenCodexLocally, LOCAL_SOURCE_PRIORITY_ID)
            }.AsReadOnly();

        #endregion

        #region Properties

        /// <summary>
        /// The last version of COMPASS that the user has run
        /// </summary>
        public SemanticVersion? LastRanVersion { get; set; }

        private ObservableCollection<PreferableFunction<Codex>> _openCodexPriority;
        /// <summary>
        /// Priority in which to try and open a code, (online or offline)
        /// </summary>
        public ObservableCollection<PreferableFunction<Codex>> OpenCodexPriority
        {
            get => _openCodexPriority;
            set => SetProperty(ref _openCodexPriority, value);
        }

        /// <summary>
        /// All codex properties that can be imported from a metadata source
        /// </summary>
        public List<CodexProperty> ImportableCodexProperties { get; init; }

        public ListLayoutPreferences ListLayoutPreferences { get; init; }
        public CardLayoutPreferences CardLayoutPreferences { get; init; }
        public TileLayoutPreferences TileLayoutPreferences { get; init; }
        public HomeLayoutPreferences HomeLayoutPreferences { get; init; }

        public UIState UIState { get; init; }
        public WindowRestoreState WindowState { get; init; }

        public UpdatePreferences UpdatePreferences { get; init; }

        private bool _autoLinkFolderTagSameName;
        public bool AutoLinkFolderTagSameName
        {
            get => _autoLinkFolderTagSameName;
            set => SetProperty(ref _autoLinkFolderTagSameName, value);
        }

        #endregion

    }
}
