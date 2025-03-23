using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.Common.Models
{
    public sealed class Tag : ObservableRecipient, ITag, IHasID, IHasChildren<Tag>, IEquatable<Tag>
    {
        public Tag() { }

        public Tag(Tag tag)
        {
            Copy(tag);
        }

        public Tag(List<Tag> allTags)
        {
            ID = Utils.GetAvailableID(allTags.Cast<IHasID>());
        }

        //Implement IHasChildren
        private ObservableCollection<Tag> _children = [];
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

        //Color bound to the UI
        public Color BackgroundColor => _internalBackgroundColor ?? Parent?.BackgroundColor ?? Colors.DarkGray;

        //Internally stored color, can be null to indicate it should follow the color of the parent tag
        private Color? _internalBackgroundColor;
        public Color? InternalBackgroundColor
        {
            get => _internalBackgroundColor;
            set
            {
                SetProperty(ref _internalBackgroundColor, value);
                OnPropertyChanged(nameof(BackgroundColor));
            }
        }

        //Implement IHasID
        private int _id = -1;
        public int ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private Tag? _parent;
        public Tag? Parent
        {
            get => _parent;
            set => SetProperty(ref _parent, value);
        }

        // Group tags are important for filtering
        // when filtering, Tags in same group get OR relation
        // Tags across groups get AND relation
        private bool _isGroup;
        public bool IsGroup
        {
            get => _isGroup;
            set => SetProperty(ref _isGroup, value);
        }
        
        private ObservableCollection<string> _linkedGlobs = new ObservableCollection<string>();

        public ObservableCollection<string> LinkedGlobs
        {
            get => _linkedGlobs;
            set => SetProperty(ref _linkedGlobs, value);
        }

        /// <summary>
        /// Does an upwards search until it find either a group tag or a root tag
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
            InternalBackgroundColor = t.InternalBackgroundColor;
            Children = new(t.Children);
            LinkedGlobs = new(t.LinkedGlobs);
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
        public static bool operator ==(Tag? lhs, Tag? rhs)
        {
            if (lhs is null)
            {
                return rhs is null; //if lhs is null, only equal if rhs is also null
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }
        public static bool operator !=(Tag? lhs, Tag? rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode() => ID.GetHashCode();
        #endregion
    }
}
