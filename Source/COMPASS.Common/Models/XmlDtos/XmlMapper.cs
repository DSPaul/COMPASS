﻿using Avalonia.Media;
using COMPASS.Common.Models.CodexProperties;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Models.XmlDtos
{
    public static class XmlMapper
    {
        #region Sanitize xml data
        //based on https://seattlesoftware.wordpress.com/2008/09/11/hexadecimal-value-0-is-an-invalid-character/
        /// <summary>
        /// Remove illegal XML characters from a string.
        /// </summary>
        private static string Sanitize(this string? str)
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
            //if preferences doesn't have file priorities, put them in default order
            if (priorityIds is null)
            {
                return new(Preferences.Preferences.OpenCodexFunctions);
            }

            return new(Preferences.Preferences.OpenCodexFunctions.OrderBy(pf =>
            {
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
                foreach (CodexPropertyDto propDto in propertyDtos)
                {
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

        #region Tag

        public static Tag ToModel(this TagDto dto, Tag? parentTag = null)
        {
            var model = new Tag()
            {
                ID = dto.ID,
                Content = dto.Content,
                InternalBackgroundColor = dto.BackgroundColor?.ToModel(),
                IsGroup = dto.IsGroup,
                Parent = parentTag,
                LinkedGlobs = new(dto.LinkedGlobs)
            };

            model.Children = new ObservableCollection<Tag>(dto.Children.Select(child => child.ToModel(model)));
            return model;
        }

        public static TagDto ToDto(this Tag model) => new()
        {
            ID = model.ID,
            Content = Sanitize(model.Content),
            BackgroundColor = model.InternalBackgroundColor?.ToDto(),
            IsGroup = model.IsGroup,
            Children = model.Children.Select(ToDto).ToList(),
            LinkedGlobs = model.LinkedGlobs.ToList()
        };

        #endregion

        #region Color

        public static Color ToModel(this ColorDto dto) =>
            Color.FromArgb((byte)dto.A, (byte)dto.R, (byte)dto.G, (byte)dto.B);


        public static ColorDto ToDto(this Color model) => new()
        {
            A = model.A,
            R = model.R,
            G = model.G,
            B = model.B,
        };

        #endregion

        #region CollectionInfo

        public static CollectionInfo ToModel(this CollectionInfoDto dto, IList<Tag> allTags)
        {
            var collectionInfo = new CollectionInfo()
            {
                BanishedPaths = new(dto.BanishedPaths),
                AutoImportFolders = new(dto.AutoImportFolders.Select(ToModel)),
                FiletypePreferences = dto.FiletypePreferences.DistinctBy(x => x.Key)
                                                             .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                                                             .ToDictionary(x => x.Key!, x => x.Value),
            };
                
            #region Migrate save data from previous versions
#pragma warning disable CS0618 // Type or member is obsolete
            
            //(1.x -> 2.x) migrate Folder - Tag Links
            foreach (FolderTagPairDto pair in dto.FolderTagPairs ?? [])
            {
                allTags.FirstOrDefault(t => t.ID == pair.TagID)?.LinkedGlobs.AddIfMissing(pair.Folder);
            }

            //(1.6.0 -> 1.7.0) migrate from AutoImportFoldersViewSource to AutoImportFolders 
            if (dto.AutoImportDirectories.SafeAny() && !dto.AutoImportFolders.Any())
            {
                collectionInfo.AutoImportFolders = new(dto.AutoImportDirectories!.Select(dir => new Folder(dir)));
            }

#pragma warning restore CS0618 // Type or member is obsolete

            #endregion
            
            return collectionInfo;
        }

        public static CollectionInfoDto ToDto(this CollectionInfo model)
        {
            return new CollectionInfoDto()
            {
                AutoImportFolders = model.AutoImportFolders.Select(ToDto).ToList(),
                BanishedPaths = model.BanishedPaths.Select(Sanitize).ToList(),
                FiletypePreferences = model.FiletypePreferences.Select(x => new KeyValuePairDto<string, bool>(x)).ToList(),
            };
        }
        
        private static Folder ToModel(this FolderDto dto)
        {
            var folder = new Folder(dto.FullPath)
            {
                HasAllSubFolders = dto.HasAllSubFolders
            };

            if (dto.SubFolders != null)
            {
                folder.SubFolders = new(dto.SubFolders.Select(ToModel));
            }
            
            return folder;
        }

        private static FolderDto ToDto(this Folder model)
        {
            return new FolderDto()
            {
                HasAllSubFolders = model.HasAllSubFolders,
                FullPath = Sanitize(model.FullPath),
                SubFolders = model.SubFolders.Select(ToDto).ToList(),
            };
        }
        #endregion
    }
}
