﻿using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public sealed class Tag : ObservableRecipient, ITag, IHasID, IHasChildren<Tag>, IEquatable<Tag>
    {
        //Empty Constructor needed for serialization
        public Tag() { }

        public Tag(Tag tag)
        {
            Copy(tag);
        }

        public Tag(List<Tag> allTags)
        {
            AllTags = allTags;
            ID = Utils.GetAvailableID(allTags.ToList<IHasID>());
        }

        //Implement IHasChildren
        private ObservableCollection<Tag> _children = new();
        public ObservableCollection<Tag> Children
        {
            get => _children;
            set
            {
                SetProperty(ref _children, value);
                foreach (var child in _children)
                {
                    child.Parent = this;
                }
            }
        }

        //implement ITag
        private string _content = "";
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value, true); //needs to broadcast so TagEdit can validate the input
        }

        // Add [XmlIgnoreAttribute] in a future update and delete the setter
        // these are only needed to load tags.xml files created before the 1.1.0 update
        public Color BackgroundColor
        {
            get => _serializableBackgroundColor ?? Parent?.BackgroundColor ?? Colors.DarkGray;
            set => _serializableBackgroundColor = value;
        }

        private Color? _serializableBackgroundColor = Colors.DarkGray;
        public Color? SerializableBackgroundColor
        {
            get => _serializableBackgroundColor;
            set
            {
                SetProperty(ref _serializableBackgroundColor, value);
                OnPropertyChanged(nameof(BackgroundColor));
            }
        }

        //Implement IHasID
        private int _id = -1;
        public int ID
        {
            get => _id;
            set
            {
                SetProperty(ref _id, value);
                foreach (var child in Children)
                {
                    child.ParentID = value;
                }
            }
        }

        // can't save parent itself, would cause infinite loop when serializing
        // so save ID instead
        public int ParentID { get; set; } = -1;
        [XmlIgnore]
        public Tag? Parent
        {
            get => ParentID == -1 ? null : AllTags.Find(tag => tag.ID == ParentID);
            set => ParentID = value?.ID ?? -1;
        }

        [XmlIgnore]
        public List<Tag> AllTags { get; set; } = new(); //needed to get parent tag from parent ID

        // Group tags are important for filtering
        // when filtering, Tags in same group get OR relation
        // Tags across groups get AND relation
        private bool _isGroup;
        public bool IsGroup
        {
            get => _isGroup;
            set => SetProperty(ref _isGroup, value);
        }

        /// <summary>
        /// Does an upwards search untill it find either a group tag or a root tag
        /// </summary>
        /// <returns></returns>
        public Tag GetGroup()
        {
            if (IsGroup || Parent is null) return this;
            return Parent.GetGroup();
        }

        #region Equal and Copy Functions
        //can use copy over ctor to retain reference
        public void Copy(Tag t)
        {
            ID = t.ID;
            Content = t.Content;
            Parent = t.Parent;
            IsGroup = t.IsGroup;
            SerializableBackgroundColor = t.SerializableBackgroundColor;
            Children = new ObservableCollection<Tag>(t.Children);
            AllTags = t.AllTags;
        }

        //Overwrite Equal operator
        public override bool Equals(object? obj) => this.Equals(obj as Tag);

        public bool Equals(Tag? other)
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
                return rhs is null; //if lhs is null, only equal if rhs is also null
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
