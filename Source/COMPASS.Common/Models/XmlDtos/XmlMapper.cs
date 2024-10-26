using COMPASS.Common.Models.CodexProperties;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace COMPASS.Common.Models.XmlDtos
{
    public static class XmlMapper
    {
        #region Sanitize xml data
        //based on https://seattlesoftware.wordpress.com/2008/09/11/hexadecimal-value-0-is-an-invalid-character/
        /// <summary>
        /// Remove illegal XML characters from a string.
        /// </summary>
        private static string Sanitize(this string str)
        {
            if (str is null) return "";

            StringBuilder buffer = new(str.Length);
            foreach (char c in str.Where(c => IsLegalXmlChar(c)))
            {
                buffer.Append(c);
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Whether a given character is allowed by XML 1.0.
        /// </summary>
        private static bool IsLegalXmlChar(int character) => character switch
        {
            0x9 => true, // '\t' == 9 
            0xA => true, // '\n' == 10         
            0xD => true, // '\r' == 13        
            (>= 0x20) and (<= 0xD7FF) => true,
            (>= 0xE000) and (<= 0xFFFD) => true,
            (>= 0x10000) and (<= 0x10FFFF) => true,
            _ => false
        };

        #endregion

        #region Codex

        public static Codex ToModel(this CodexDto dto, IList<Tag> tags)
        {
            Codex codex = new()
            {
                // COMPASS related Metadata
                ID = dto.ID,
                ThumbnailPath = dto.ThumbnailPath,
                CoverArtPath = dto.CoverArtPath,

                //Codex related Metadata
                Title = dto.Title,
                SortingTitle = dto.SortingTitle,
                Authors = new(dto.Authors),
                Publisher = dto.Publisher,
                Description = dto.Description,
                ReleaseDate = dto.ReleaseDate,
                PageCount = dto.PageCount,
                Version = dto.Version,

                //User related Metadata
                PhysicallyOwned = dto.PhysicallyOwned,
                Rating = dto.Rating,
                Favorite = dto.Favorite,

                //User behaviour metadata
                DateAdded = dto.DateAdded,
                LastOpened = dto.LastOpened,
                OpenedCount = dto.OpenedCount,

                //Sources
                Sources = new SourceSet()
                {
                    SourceURL = dto.SourceURL,
                    Path = dto.Path,
                    ISBN = dto.ISBN,
                }
            };

            codex.Tags = new(tags.Where(tag => dto.TagIDs.Contains(tag.ID)));

            return codex;
        }

        public static CodexDto ToDto(this Codex model)
        {
            CodexDto dto = new()
            {
                // COMPASS related Metadata
                ID = model.ID,
                ThumbnailPath = model.ThumbnailPath.Sanitize(),
                CoverArtPath = model.CoverArtPath.Sanitize(),

                //Codex related Metadata
                Title = model.Title.Sanitize(),
                SortingTitle = model.UserDefinedSortingTitle.Sanitize(),
                Authors = model.Authors.Select(author => author.Sanitize()).ToList(),
                Publisher = model.Publisher.Sanitize(),
                Description = model.Description.Sanitize(),
                ReleaseDate = model.ReleaseDate,
                PageCount = model.PageCount,
                Version = model.Version,

                //User related Metadata
                PhysicallyOwned = model.PhysicallyOwned,
                Rating = model.Rating,
                Favorite = model.Favorite,
                TagIDs = model.Tags.Select(t => t.ID).ToList(),

                //User behaviour metadata
                DateAdded = model.DateAdded,
                LastOpened = model.LastOpened,
                OpenedCount = model.OpenedCount,

                //Sources
                SourceURL = model.Sources.SourceURL.Sanitize(),
                Path = model.Sources.Path.Sanitize(),
                ISBN = model.Sources.ISBN.Sanitize(),
            };

            return dto;
        }

        #endregion

        #region Preferences

        public static Preferences.Preferences ToModel(this PreferencesDto dto)
        {
            var model = new Preferences.Preferences()
            {
                OpenCodexPriority = MapCodexPriorities(dto.OpenFilePriorityIDs),
                CodexProperties = dto.CodexProperties.ToModels(),
                ListLayoutPreferences = dto.ListLayoutPreferences,
                CardLayoutPreferences = dto.CardLayoutPreferences,
                TileLayoutPreferences = dto.TileLayoutPreferences,
                HomeLayoutPreferences = dto.HomeLayoutPreferences,
                AutoLinkFolderTagSameName = dto.AutoLinkFolderTagSameName,
                UIState = dto.UIState,
            };
            return model;
        }

        public static PreferencesDto ToDto(this Preferences.Preferences prefs)
        {
            PreferencesDto dto = new()
            {
                OpenFilePriorityIDs = prefs.OpenCodexPriority.Select(pf => pf.ID).ToList(),
                CodexProperties = prefs.CodexProperties.Select(prop => prop.ToDto()).ToList(),
                ListLayoutPreferences = prefs.ListLayoutPreferences,
                CardLayoutPreferences = prefs.CardLayoutPreferences,
                TileLayoutPreferences = prefs.TileLayoutPreferences,
                HomeLayoutPreferences = prefs.HomeLayoutPreferences,
                UIState = prefs.UIState,
                AutoLinkFolderTagSameName = prefs.AutoLinkFolderTagSameName,
            };

            return dto;
        }

        /// <summary>
        /// Map Codex open priority from dto
        /// </summary>
        /// <param name="priorityIds"></param>
        /// <returns></returns>
        private static ObservableCollection<PreferableFunction<Codex>> MapCodexPriorities(List<int>? priorityIds)
        {
            if (priorityIds is null)
            {
                return new(Preferences.Preferences.OpenCodexFunctions);
            }

            return new(Preferences.Preferences.OpenCodexFunctions.OrderBy(pf =>
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
        private static List<CodexProperty> ToModels(this List<CodexPropertyDto> propertyDtos)
        {
#pragma warning disable CS0618 // Type or member "Label" is obsolete

            //In versions 1.6.0 and lower, label was stored instead of name
            var useLabel = propertyDtos.All(prop => string.IsNullOrEmpty(prop.Name) && !string.IsNullOrEmpty(prop.Label));
            if (useLabel)
            {
                for (int i = 0; i < propertyDtos.Count; i++)
                {
                    CodexPropertyDto propDto = propertyDtos[i];
                    var foundProp = Codex.MetadataProperties.Find(p => p.Label == propDto.Label);
                    if (foundProp != null)
                    {
                        propDto.Name = foundProp.Name;
                    }
                }
            }
#pragma warning restore CS0618 // Type or member "Label" is obsolete

            var props = new List<CodexProperty>();

            foreach (var defaultProp in Codex.MetadataProperties)
            {
                CodexPropertyDto? propDto = propertyDtos.Find(p => p.Name == defaultProp.Name);
                // Add Preferences from defaults if they weren't found on the loaded Preferences
                CodexProperty? prop = propDto is null ? defaultProp : propDto.ToModel();
                if (prop is not null)
                {
                    props.Add(prop);
                }
            }

            return props;
        }

        #region CodexProperty
        public static CodexProperty? ToModel(this CodexPropertyDto propDto)
        {
            var prop = CodexProperty.GetInstance(propDto.Name);

            if (prop == null)
            {
                return null;
            }

            prop.SourcePriority = propDto.SourcePriority;
            prop.OverwriteMode = propDto.OverwriteMode;

            return prop;
        }

        public static CodexPropertyDto ToDto(this CodexProperty prop)
        {
            CodexPropertyDto dto = new()
            {
                Name = prop.Name,
                OverwriteMode = prop.OverwriteMode,
                //Use order from NameMetaDataSources (which was reordered by user)
                SourcePriority = prop.SourcePriorityNamed.Select(namedSource => namedSource.Source).ToList(),
            };

            return dto;
        }

        #endregion

        #endregion
    }
}
