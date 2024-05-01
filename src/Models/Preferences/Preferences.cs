using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Models.XmlDtos;
using COMPASS.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.Models.Preferences
{
    public class Preferences : ObservableObject
    {
        public Preferences()
        {
            _openCodexPriority = new(_openCodexFunctions);
            CodexProperties = Codex.Properties.ToList();
            ListLayoutPreferences = new();
            CardLayoutPreferences = new();
            TileLayoutPreferences = new();
            HomeLayoutPreferences = new();
            UIState = new UIState();
            AutoLinkFolderTagSameName = true;
        }

        public Preferences(PreferencesDto dto)
        {
            _openCodexPriority = MapCodexPriority(dto.OpenFilePriorityIDs);
            CodexProperties = MapCodexProperties(dto.CodexProperties);
            ListLayoutPreferences = dto.ListLayoutPreferences;
            CardLayoutPreferences = dto.CardLayoutPreferences;
            TileLayoutPreferences = dto.TileLayoutPreferences;
            HomeLayoutPreferences = dto.HomeLayoutPreferences;
            AutoLinkFolderTagSameName = dto.AutoLinkFolderTagSameName;
            UIState = dto.UIState;
        }

        #region Constants

        //list with possible functions to open a file
        private static readonly List<PreferableFunction<Codex>> _openCodexFunctions = new()
            {
                new PreferableFunction<Codex>("Web Version", CodexViewModel.OpenCodexOnline,0),
                new PreferableFunction<Codex>("Local File", CodexViewModel.OpenCodexLocally,1)
            };
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

        public bool AutoLinkFolderTagSameName
        {
            get => Properties.Settings.Default.AutoLinkFolderTagSameName;
            set
            {
                Properties.Settings.Default.AutoLinkFolderTagSameName = value;
                OnPropertyChanged();
            }
        }



        #endregion

        #region Mapping

        /// <summary>
        /// Map Codex open priority from dto
        /// </summary>
        /// <param name="priorityIds"></param>
        /// <returns></returns>
        private static ObservableCollection<PreferableFunction<Codex>> MapCodexPriority(List<int>? priorityIds)
        {
            if (priorityIds is null)
            {
                return new(_openCodexFunctions);
            }

            return new(_openCodexFunctions.OrderBy(pf =>
            {
                //if preferences doesn't have file priorities, put them in default order
                if (priorityIds is null)
                {
                    return pf.ID;
                }

                //get index in user preference
                int index = priorityIds.IndexOf(pf.ID);

                //if it was not found in preference, use its default ID
                if (index < 0)
                {
                    return pf.ID;
                }

                return index;
            }));
        }

        /// <summary>
        /// Map codex metadata properties from dto
        /// </summary>
        /// <param name="propertyDtos"></param>
        /// <returns></returns>
        private static List<CodexProperty> MapCodexProperties(List<CodexPropertyDto> propertyDtos)
        {
#pragma warning disable CS0612 // Type or member "Label" is obsolete

            //In versions 1.6.0 and lower, label was stored instead of name
            var useLabel = propertyDtos.All(prop => string.IsNullOrEmpty(prop.Name) && !string.IsNullOrEmpty(prop.Label));
            if (useLabel)
            {
                for (int i = 0; i < propertyDtos.Count; i++)
                {
                    CodexPropertyDto propDto = propertyDtos[i];
                    var foundProp = Codex.Properties.Find(p => p.Label == propDto.Label);
                    if (foundProp != null)
                    {
                        propDto.Name = foundProp.Name;
                    }
                }
            }
#pragma warning restore CS0612 // Type or member "Label" is obsolete

            var props = new List<CodexProperty>();

            foreach (var defaultProp in Codex.Properties)
            {
                CodexPropertyDto? propDto = propertyDtos.Find(p => p.Name == defaultProp.Name);
                // Add Preferences from defaults if they weren't found on the loaded Preferences
                CodexProperty prop = propDto is null ? defaultProp : new(propDto, defaultProp);
                props.Add(prop);
            }

            return props;
        }

        public PreferencesDto ToDto()
        {
            PreferencesDto dto = new()
            {
                OpenFilePriorityIDs = OpenCodexPriority.Select(pf => pf.ID).ToList(),
                CodexProperties = CodexProperties.Select(prop => prop.ToDto()).ToList(),
                ListLayoutPreferences = ListLayoutPreferences,
                CardLayoutPreferences = CardLayoutPreferences,
                TileLayoutPreferences = TileLayoutPreferences,
                HomeLayoutPreferences = HomeLayoutPreferences,
                UIState = UIState,
                AutoLinkFolderTagSameName = AutoLinkFolderTagSameName,
            };

            return dto;
        }

        #endregion

    }
}
