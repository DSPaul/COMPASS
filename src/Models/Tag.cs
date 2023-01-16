using COMPASS.Tools;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public class Tag : ObservableObject, IHasID, IHasChilderen<Tag>
    {
        //Emtpy Contructor needed for serialization
        public Tag() { }

        public Tag(List<Tag> alltags)
        {
            AllTags = alltags;
            ID = Utils.GetAvailableID(alltags.ToList<IHasID>());
        }

        //needed to get parent tag from parent ID
        [XmlIgnoreAttribute]
        public List<Tag> AllTags;

        private ObservableCollection<Tag> _childeren = new();
        public ObservableCollection<Tag> Children
        {
            get { return _childeren; }
            set { SetProperty(ref _childeren, value); }
        }

        private string _content = "";
        public virtual string Content
        {
            get { return _content; }
            set { SetProperty(ref _content, value); }
        }

        private int _parentID = -1;
        public int ParentID
        {
            get { return _parentID; }
            set { SetProperty(ref _parentID, value); }
        }

        private Color _backgroundColor = Colors.Black;
        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set { SetProperty(ref _backgroundColor, value); }
        }

        private bool _isGroup;
        public bool IsGroup
        {
            get { return _isGroup; }
            set { SetProperty(ref _isGroup, value); }
        }

        public int ID { get; set; }

        //can't save parent itself, would cause infinite loop when serializing
        public Tag GetParent()
        {
            if (ParentID == -1) return null;
            return AllTags.First(tag => tag.ID == ParentID);
        }

        //returns the first parent that is a group or null if no parents are group
        public virtual object GetGroup()
        {
            if (IsGroup) return this;
            if (ParentID == -1) return null;
            Tag temp = this.GetParent();
            while (!temp.IsGroup)
            {
                if (temp.ParentID != -1) temp = temp.GetParent();
                else break;
            }
            return temp;
        }

        #region Equal and Copy Fucntions
        public void Copy(Tag t)
        {
            ID = t.ID;
            Content = t.Content;
            ParentID = t.ParentID;
            IsGroup = t.IsGroup;
            BackgroundColor = t.BackgroundColor;
            Children = new ObservableCollection<Tag>(t.Children);
            AllTags = t.AllTags;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj is Tag objAsTag && Equals(objAsTag);
        }

        public bool Equals(Tag other)
        {
            if (other == null) return false;
            return (this.ID.Equals(other.ID));
        }

        public override int GetHashCode()
        {
            return ID;
        }
        #endregion
    }
}
