using COMPASS.ViewModels.Sources;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace COMPASS.Models
{

    /// <summary>
    /// Class that contains all the global preferences, ready to be serialized
    /// </summary>
    [XmlRoot("SerializablePreferences")]
    public class GlobalPreferences
    {
        public List<int> OpenFilePriorityIDs { get; set; }
        public List<MetaDataPreference> MetaDataPreferences { get; set; } = new();

        private Dictionary<string, List<NamedImportSource>> DefaultMetaDataPrefs = new()
        {
            { "Title", new()
                {
                    new(ImportSource.File),
                    new(ImportSource.GmBinder),
                    new(ImportSource.Homebrewery),
                    new(ImportSource.GoogleDrive),
                    new(ImportSource.ISBN),
                    new(ImportSource.GenericURL)
                }
            },
            { "Authors", new()
                {
                    new(ImportSource.File),
                    new(ImportSource.GmBinder),
                    new(ImportSource.Homebrewery),
                    new(ImportSource.ISBN),
                    new(ImportSource.GenericURL)
                }
            },
            { "Publisher", new()
                {
                    new(ImportSource.ISBN),
                }
            },
            { "Version", new()
                {
                    new(ImportSource.Homebrewery),
                }
            },
            { "Pagecount", new()
                {
                    new(ImportSource.File),
                    new(ImportSource.GmBinder),
                    new(ImportSource.Homebrewery),
                    new(ImportSource.ISBN),
                }
            },
            { "Cover Art", new()
                {
                    new(ImportSource.File),
                    new(ImportSource.GmBinder),
                    new(ImportSource.Homebrewery),
                    new(ImportSource.GoogleDrive),
                    new(ImportSource.ISBN),
                }
            },
            { "Tags", new()
                {
                    new(ImportSource.Folder),
                }
            },
            { "Description", new()
                {
                    new(ImportSource.Homebrewery),
                    new(ImportSource.ISBN),
                    new(ImportSource.GenericURL),
                }
            },
            { "Release Date", new()
                {
                    new(ImportSource.Homebrewery),
                    new(ImportSource.ISBN),
                }
            }
        };

        public void Init()
        {
            foreach (var entry in DefaultMetaDataPrefs)
            {
                var preference = MetaDataPreferences.FirstOrDefault(pref => pref.Label == entry.Key);
                // Add Preferences from defaults if they weren't found on the loaded Preferences
                if (preference is null)
                {
                    preference = new(entry.Key);
                    MetaDataPreferences.Add(preference);
                }

                //Call init on every preference
                preference.Init(entry.Value);
            }
        }
    }
}
