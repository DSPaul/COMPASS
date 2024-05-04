using CommunityToolkit.Mvvm.ComponentModel;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public class FolderTagPair : ObservableObject
    {
        //empty ctor for deserialization
        public FolderTagPair() { }

        public FolderTagPair(string folder, Tag tag)
        {
            Folder = folder;
            Tag = tag;
        }

        private string _folder = "";
        public string Folder
        {
            get => _folder;
            set => SetProperty(ref _folder, value);
        }

        private Tag? _tag;
        [XmlIgnore]
        public Tag? Tag
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

        #region override Equal operator
        public override bool Equals(object? obj) => this.Equals(obj as FolderTagPair);

        public bool Equals(FolderTagPair? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (GetType() != other.GetType())
                return false;
            return Folder == other.Folder
                && Tag is not null
                && other.Tag is not null
                && Tag! == other.Tag!;
        }
        public static bool operator ==(FolderTagPair lhs, FolderTagPair rhs)
        {
            if (lhs is null)
            {
                return rhs is null; //if lhs is null, only equal if rhs is also null
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }
        public static bool operator !=(FolderTagPair lhs, FolderTagPair rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            var hash = Folder.GetHashCode();
            if (Tag is not null)
            {
                hash += Tag.GetHashCode();
            }
            return hash;
        }
        #endregion
    }
}
