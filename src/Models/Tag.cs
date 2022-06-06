using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public class Tag : ObservableObject, IHasID, IHasChilderen<Tag>
    {
        //Emtpy Contructor needed for serialization
        public Tag()
        {
            Children = new ObservableCollection<Tag>();
        }

        public Tag(List<Tag> alltags)
        {
            AllTags = alltags;
            ID = Utils.GetAvailableID(alltags.ToList<IHasID>());
            Children = new ObservableCollection<Tag>();

            //set a default color for add tag
            BackgroundColor = Colors.Black;
        }

        //needed to get parent tag from parent ID
        [XmlIgnoreAttribute]
        public List<Tag> AllTags;

        private ObservableCollection<Tag> _Childeren;

        private string _Content = "";
        private int _ParentID = -1;
        private Color _BackgroundColor;
        private bool _isGroup;

        #region Getter and Setters
        public string Content
        {
            get { return _Content; }
            set { SetProperty(ref _Content, value); }
        }

        public int ID{ get; set; }

        public int ParentID
        {
            get { return _ParentID; }
            set { SetProperty(ref _ParentID, value); }
        }

        public Color BackgroundColor
        {
            get { return _BackgroundColor; }
            set { SetProperty(ref _BackgroundColor, value); }
        }

        public bool IsGroup
        {
            get { return _isGroup; }
            set { SetProperty(ref _isGroup, value); }
        }

        public ObservableCollection<Tag> Children
        {
            get { return _Childeren; }
            set { SetProperty(ref _Childeren, value); }
        }
        #endregion

        //can't save parent itself, would cause infinite loop when serializing
        public Tag GetParent()
        {
            if (ParentID == -1) return null;
            return AllTags.First(tag => tag.ID == ParentID);
        }

        public virtual object GetGroup()
        //returns the first parent that is a group or null if no parents are group
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
