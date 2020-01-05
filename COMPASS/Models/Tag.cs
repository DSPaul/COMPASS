using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace COMPASS
{
    public class Tag
    {
        public Tag()
        {
            try
            {
                int tempID = 0;
                while (UserSettings.CurrentData.AllTags.Any(t => t.ID == tempID))
                {
                    tempID++;
                }
                ID = tempID;
            }

            catch { }
            this.Items = new ObservableCollection<Tag>();
        }

        public void Copy(Tag t)
        {
            ID = t.ID;
            Content = t.Content;
            ParentID = t.ParentID;
            Check = t.Check;
            Expanded = t.Expanded;
            BackgroundColor = t.BackgroundColor;
            Items = new ObservableCollection<Tag>(t.Items);
        }

        public ObservableCollection<Tag> Items { get; set; }

        public int ID { get; set; }
        public string Content { get; set; }
        public int ParentID { get; set; } = -1;
        public bool Check { get; set; }
        public bool Expanded { get; set; }
        public Color BackgroundColor { get; set; }

        public Tag GetParent()
        {
            if (ParentID == -1) return null;
            return UserSettings.CurrentData.AllTags.First(par => par.ID == ParentID);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Tag objAsTag = obj as Tag;
            if (objAsTag == null) return false;
            else return Equals(objAsTag);
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
    }
}
