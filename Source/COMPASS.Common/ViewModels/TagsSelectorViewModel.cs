using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Tools;

namespace COMPASS.Common.ViewModels
{
    //TODO make this use the more generic HierachicalSelectorViewmodel
    public class TagsSelectorViewModel : ViewModelBase
    {
        public TagsSelectorViewModel(List<CodexCollection> collections)
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
            public TagCollection(CodexCollection c)
            {
                Name = c.Name;
                _collection = c;
            }

            private CodexCollection _collection;

            public string Name { get; set; }

            private CheckableTreeNode<Tag>? _tagsRoot = null;
            public CheckableTreeNode<Tag> TagsRoot
            {
                get
                {
                    //Lazy load, only load the first time
                    if (_tagsRoot != null) return _tagsRoot;

                    //load if not done yet
                    if (!_collection.AllTags.Any()) ServiceResolver.Resolve<ICodexCollectionStorageService>().LoadTags(_collection);
                    //convert to nodes
                    _tagsRoot = new CheckableTreeNode<Tag>(new Tag(), containerOnly: true)
                    {
                        Children = new(_collection.RootTags
                            .Select(t => new CheckableTreeNode<Tag>(t, containerOnly: t.IsGroup)))
                    };
                    //init expanded, checked and container only
                    foreach (var node in _tagsRoot.Children.Flatten())
                    {
                        node.Expanded = node.Item.IsGroup;
                        node.ContainerOnly = node.Item.IsGroup;
                        node.IsChecked = false;
                    }
                    _tagsRoot.Updated += _ => OnPropertyChanged(nameof(ImportCount));
                    return _tagsRoot;
                }
            }

            public int ImportCount => CheckableTreeNode<Tag>.GetCheckedItems(TagsRoot.Children).Flatten().Count();
        }
    }
}
