using System.Xml.Serialization;

namespace COMPASS.Models
{
    public class FolderTagPair : ObservableObject
    {
        public FolderTagPair() { }

        public FolderTagPair(string folder, Tag tag)
        {
            Folder = folder;
            Tag = tag;
        }

        private string _folder;
        public string Folder
        {
            get => _folder;
            set => SetProperty(ref _folder, value);
        }

        private Tag _tag;
        [XmlIgnore]
        public Tag Tag
        {
            get => _tag;
            set
            {
                if (value is null) return;
                TagID = value.ID;
                SetProperty(ref _tag, value);
            }
        }

        //ID for serialisation
        public int TagID { get; set; }

        public void InitTag(CodexCollection collection) => Tag = collection.AllTags.Find(t => TagID == t.ID);
    }
}
