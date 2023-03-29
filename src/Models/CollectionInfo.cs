using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    /// <summary>
    /// Contains all the Info on a Collection that needs to be serialized
    /// </summary>
    public class CollectionInfo
    {
        //Folders to check for new files
        public ObservableCollection<string> AutoImportDirectories { get; set; } = new();
        public ObservableCollection<string> BanishedPaths { get; set; } = new();


        private Dictionary<string, bool> _filetypePreferences;
        [XmlIgnore]
        public Dictionary<string, bool> FiletypePreferences
        {
            get => _filetypePreferences ??= SerializableFiletypePreferences.ToDictionary(x => x.Key, x => x.Value);
            set => _filetypePreferences = value;
        }

        [XmlArray(ElementName = "FiletypePreferences")]
        [XmlArrayItem(ElementName = "FileType")]
        public List<ObservableKeyValuePair<string, bool>> SerializableFiletypePreferences { get; set; } = new(); //needed because Dicts cannot be serialized

        //Copies the dict into the serializable version
        public void PrepareSave() => SerializableFiletypePreferences = FiletypePreferences.Select(x => new ObservableKeyValuePair<string, bool>(x)).ToList();
    }
}
