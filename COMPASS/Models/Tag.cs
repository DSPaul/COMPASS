using COMPASS.Models;
using COMPASS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace COMPASS.Models
{
    public class Tag : ObservableObject
    {
        //Contructor
        public Tag()
        {

        }

        public Tag(ObservableCollection<Tag> alltags)
        {
            _allTags = alltags;
            int tempID = 0;
            while (_allTags.Any(t => t.ID == tempID))
            {
                tempID++;
            }
            ID = tempID;
            this.Items = new ObservableCollection<Tag>();
        }

        private ObservableCollection<Tag> _allTags;

        private ObservableCollection<Tag> _Items;

        private int _ID;
        private string _Content = "";
        private int _ParentID = -1;
        private Color _BackgroundColor;
        private bool _isGroup;

        #region Getter and Setters
        public ObservableCollection<Tag> Items
        {
            get { return _Items; }
            set { SetProperty(ref _Items, value); }
        }

        public int ID
        {
            get { return _ID; }
            set { SetProperty(ref _ID, value); }
        }
        public string Content
        {
            get { return _Content; }
            set { SetProperty(ref _Content, value); }
        }
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
        #endregion

        public Tag GetParent()
        {
            if (ParentID == -1) return null;
            return _allTags.First(par => par.ID == ParentID);
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

        public void SetAllTags(ObservableCollection<Tag> at)
        {
            _allTags = at;
        }

        #region Equal and Copy Fucntions
        public void Copy(Tag t)
        {
            ID = t.ID;
            Content = t.Content;
            ParentID = t.ParentID;
            IsGroup = t.IsGroup;
            BackgroundColor = t.BackgroundColor;
            Items = new ObservableCollection<Tag>(t.Items);
            _allTags = t._allTags;
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
