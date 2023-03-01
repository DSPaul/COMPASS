using COMPASS.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public sealed class Tag : ObservableObject, ITag, IHasID, IHasChilderen<Tag>, IEquatable<Tag>
    {
        //Emtpy Contructor needed for serialization
        public Tag() { }

        public Tag(List<Tag> alltags)
        {
            AllTags = alltags;
            ID = Utils.GetAvailableID(alltags.ToList<IHasID>());
        }

        //Implement IHasChilderen
        private ObservableCollection<Tag> _childeren = new();
        public ObservableCollection<Tag> Children
        {
            get => _childeren;
            set => SetProperty(ref _childeren, value);
        }

        //implement ITag
        private string _content = "";
        public string Content
        {
            get => _content;
            set
            {
                value = Utils.SanitizeXmlString(value);
                SetProperty(ref _content, value);
            }
        }

        private Color _backgroundColor = Colors.Black;
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }

        //Implement IHasID
        public int ID { get; set; }

        // can't save parent itself, would cause infinite loop when serializing
        // so save ID instead
        public int ParentID { get; set; } = -1;
        [XmlIgnoreAttribute]
        public Tag Parent
        {
            get
            {
                if (ParentID == -1) return null;
                return AllTags.First(tag => tag.ID == ParentID);
            }

            set
            {
                if (value is null) ParentID = -1;
                else ParentID = value.ID;
            }
        }

        [XmlIgnoreAttribute]
        public List<Tag> AllTags { get; set; } //needed to get parent tag from parent ID


        // Group tags are important for filtering
        // when filtering, Tags in same group get OR relation
        // Tags across groups get AND relation
        private bool _isGroup;
        public bool IsGroup
        {
            get => _isGroup;
            set => SetProperty(ref _isGroup, value);
        }

        //returns the first parent that is a group
        //or Root parent if no parents are group
        //or null if it is a root tag
        public Tag GetGroup()
        {
            if (IsGroup) return this;
            if (Parent is null) return null;
            Tag temp = Parent;
            while (!temp.IsGroup)
            {
                if (temp.Parent != null) temp = temp.Parent;
                else break;
            }
            return temp;
        }

        #region Equal and Copy Functions
        public void Copy(Tag t)
        {
            ID = t.ID;
            Content = t.Content;
            Parent = t.Parent;
            IsGroup = t.IsGroup;
            BackgroundColor = t.BackgroundColor;
            Children = new ObservableCollection<Tag>(t.Children);
            AllTags = t.AllTags;
        }

        //Overwrite Equal operator
        public override bool Equals(object obj) => this.Equals(obj as Tag);

        public bool Equals(Tag other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (this.GetType() != other.GetType())
                return false;
            return ID == other.ID;
        }
        public static bool operator ==(Tag lhs, Tag rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }
        public static bool operator !=(Tag lhs, Tag rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode() => ID.GetHashCode();
        #endregion
    }
}
