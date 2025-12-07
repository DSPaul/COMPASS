using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.ViewModels.Selection
{
    //TODO make this use the more generic HierachicalSelectorViewmodel
    public class TagsSelectorViewModel : ViewModelBase
    {
        public TagsSelectorViewModel(IEnumerable<CodexCollection> collections)
        {
            TagCollections = collections.Select(c => new TagCollection(c)).ToList();
            SelectedTagCollection = TagCollections.FirstOrDefault();
        }

        public TagsSelectorViewModel(CodexCollection collection) : this([collection]) { }

        
        private List<TagCollection> _tagCollections = [];
        public List<TagCollection> TagCollections
        {
            get => _tagCollections;
            set => SetProperty(ref _tagCollections, value);
        }

        private TagCollection? _selectedTagCollection;
        /// <summary>
        /// Can be null if no collections were present in the satchel
        /// </summary>
        public TagCollection? SelectedTagCollection
        {
            get => _selectedTagCollection;
            set => SetProperty(ref _selectedTagCollection, value);
        }

        public bool HasTags => TagCollections.Any(tc => tc.TagsRoot.Children.Any());
        
        public class TagCollection : ObservableObject
        {
            public TagCollection(CodexCollection collection)
            {
                _collection = collection;
                Name = collection.Name;
            }

            private readonly CodexCollection _collection;

            public string Name { get; set; }

            private CheckableTreeNode<Tag>? _tagsRoot = null;
            public CheckableTreeNode<Tag> TagsRoot
            {
                get
                {
                    //Lazy load, only load the first time
                    if (_tagsRoot != null) return _tagsRoot;
                    
                    //convert to nodes
                    _tagsRoot = new CheckableTreeNode<Tag>(new Tag(), containerOnly: true, propagateChanges: true)
                    {
                        Children = new(_collection.RootTags
                            .Select(t => new CheckableTreeNode<Tag>(t, containerOnly: t.IsGroup, propagateChanges: true)))
                    };
                    //init expanded, checked and container only
                    foreach (var node in _tagsRoot.Children.Flatten())
                    {
                        node.Expanded = node.Item.IsGroup;
                        node.ContainerOnly = node.Item.IsGroup;
                        node.IsChecked = false;
                    }
                    _tagsRoot.Updated += (_, _) => OnPropertyChanged(nameof(ImportCount));
                    return _tagsRoot;
                }
            }

            public int ImportCount => CheckableTreeNode<Tag>.GetCheckedItems(TagsRoot.Children).Flatten().Count();
        }
    }
}
