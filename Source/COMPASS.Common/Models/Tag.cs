using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Tools;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.Common.Models
{
    public sealed class Tag : ObservableRecipient, IHasID, IHasChildren<Tag>
    {
        public Tag() { }

        public Tag(Tag tag)
        {
            CopyFrom(tag);
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
        
        private string _content = "";
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
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
            set
            {
                if (SetProperty(ref _parent, value) && InternalBackgroundColor == null)
                {
                    OnPropertyChanged(nameof(BackgroundColor));
                }
            }
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


        //can use copy over ctor to retain reference
        public void CopyFrom(Tag t)
        {
            ID = t.ID;
            Content = t.Content;
            Parent = t.Parent;
            IsGroup = t.IsGroup;
            InternalBackgroundColor = t.InternalBackgroundColor;
            Children = new(t.Children);
            LinkedGlobs = new(t.LinkedGlobs);
        }
    }
}
