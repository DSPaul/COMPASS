using COMPASS.Models;
using COMPASS.Tools;
using System.Collections.Generic;
using System.Linq;

namespace COMPASS.ViewModels
{
    public class TagsSelectorViewModel : ViewModelBase
    {
        public TagsSelectorViewModel(List<CodexCollection> collections)
        {
            TagCollections = collections.Select(c => new TagCollection(c)).ToList();
            SelectedTagCollection = TagCollections.FirstOrDefault();
        }

        public TagsSelectorViewModel(CodexCollection collection) : this(new List<CodexCollection> { collection }) { }

        private List<TagCollection> _tagCollections = new();
        public List<TagCollection> TagCollections
        {
            get => _tagCollections;
            set => SetProperty(ref _tagCollections, value);
        }

        private TagCollection _selectedTagCollection;
        public TagCollection SelectedTagCollection
        {
            get => _selectedTagCollection;
            set => SetProperty(ref _selectedTagCollection, value);
        }

        public class TagCollection : ObservableObject
        {
            public TagCollection(CodexCollection c)
            {
                Name = c.DirectoryName;
                _collection = c;
            }

            private CodexCollection _collection;

            public string Name { get; set; }

            public CheckableTreeNode<Tag> _tagsRoot = null;
            public CheckableTreeNode<Tag> TagsRoot
            {
                get
                {
                    //Lazy load, only load the first time
                    if (_tagsRoot != null) return _tagsRoot;

                    //load
                    _collection.LoadTags();
                    //convert to treenodes
                    _tagsRoot = new CheckableTreeNode<Tag>();
                    _tagsRoot.Children = new(_collection.RootTags
                        .Select(t => new CheckableTreeNode<Tag>(t)));
                    //init expanded and checked
                    foreach (var node in Utils.FlattenTree(_tagsRoot.Children))
                    {
                        node.Expanded = node.Item.IsGroup;
                        node.IsChecked = false;
                    }
                    _tagsRoot.Updated += _ => RaisePropertyChanged(nameof(ImportCount));
                    return _tagsRoot;
                }
            }

            public int ImportCount
            {
                get
                {
                    if (TagsRoot == null) return 0;
                    return Utils.FlattenTree(CheckableTreeNode<Tag>.GetCheckedItems(TagsRoot.Children)).Count();
                }
            }
        }
    }
}
