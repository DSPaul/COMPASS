using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Services;
using COMPASS.Infra.Models.Interfaces;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Models
{
    public sealed class Tag : ObservableRecipient, IHasId, IHasChildren<Tag>, ICloneable<Tag>
    {
        public Tag() { }

        public Tag(IEnumerable<Tag> allTags)
        {
            Id = Utils.GetAvailableId(allTags.Cast<IHasId>());
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
        
        private string _name = "";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string LongName => $"{Parent?.LongName}{(Parent == null ? "" : " > ")}{Name}";
        
        //Internally stored color, can be null to indicate it should follow the color of the parent tag
        private Color? _internalBackgroundColor;
        public Color? InternalBackgroundColor
        {
            get => _internalBackgroundColor;
            set => SetProperty(ref _internalBackgroundColor, value);
        }
        
        public Color BackgroundColor => _internalBackgroundColor ?? Parent?.BackgroundColor ?? Colors.DarkGray;
        
        private int _id = -1;
        public int Id
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
        
        private ObservableCollection<string> _linkedGlobs = [];
        public ObservableCollection<string> LinkedGlobs
        {
            get => _linkedGlobs;
            set => SetProperty(ref _linkedGlobs, value);
        }
        
        public List<string> CalculatedLinkedGlobs => PreferencesService.GetInstance().Preferences.AutoLinkFolderTagSameName ? [$"**/{Name}/**"] : [];

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
            Id = t.Id;
            Name = t.Name;
            Parent = t.Parent;
            IsGroup = t.IsGroup;
            InternalBackgroundColor = t.InternalBackgroundColor;
            Children = new(t.Children);
            LinkedGlobs = new(t.LinkedGlobs);
        }
        
        public Tag Clone()
        {
            var clone = new Tag();
            clone.CopyFrom(this);
            return clone;
        }
    }
}
