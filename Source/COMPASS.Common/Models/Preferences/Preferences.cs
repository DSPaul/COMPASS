using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Operations;

namespace COMPASS.Common.Models.Preferences
{
    public class Preferences : ObservableObject
    {
        public Preferences()
        {
            _openCodexPriority = new(OpenCodexFunctions);
            CodexProperties = Codex.MetadataProperties.ToList();
            ListLayoutPreferences = new();
            CardLayoutPreferences = new();
            TileLayoutPreferences = new();
            HomeLayoutPreferences = new();
            UIState = new UIState();
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

        private ObservableCollection<PreferableFunction<Codex>> _openCodexPriority;
        /// <summary>
        /// Priority in which to try and open a code, (online or offline)
        /// </summary>
        public ObservableCollection<PreferableFunction<Codex>> OpenCodexPriority
        {
            get => _openCodexPriority;
            set => SetProperty(ref _openCodexPriority, value);
        }

        public List<CodexProperty> CodexProperties { get; set; }

        public ListLayoutPreferences ListLayoutPreferences { get; set; }
        public CardLayoutPreferences CardLayoutPreferences { get; set; }
        public TileLayoutPreferences TileLayoutPreferences { get; set; }
        public HomeLayoutPreferences HomeLayoutPreferences { get; set; }

        public UIState UIState { get; set; }

        private bool _autoLinkFolderTagSameName;
        public bool AutoLinkFolderTagSameName
        {
            get => _autoLinkFolderTagSameName;
            set => SetProperty(ref _autoLinkFolderTagSameName, value);
        }

        #endregion

    }
}
