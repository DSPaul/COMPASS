using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.ViewModels.Selection
{
    //TODO make this use the more generic HierachicalSelectorViewmodel
    public class TagsSelectorViewModel : ViewModelBase
    {
        #region ctor
        public TagsSelectorViewModel(IEnumerable<CodexCollectionVM> collectionVms)
        {
            TagCollections = collectionVms.Select(c => new TagCollection(c)).ToList();
            SelectedTagCollection = TagCollections.FirstOrDefault();
        }

        public TagsSelectorViewModel(CodexCollectionVM collectionVm) : this([collectionVm]) { }

        #endregion

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
            public TagCollection(CodexCollectionVM collectionVm)
            {
                _collectionVm = collectionVm;
                Name = collectionVm.Identifier;
            }

            private readonly CodexCollectionVM _collectionVm;

            public string Name { get; set; }

            private CheckableTreeNode<TagViewModel>? _tagsRoot = null;
            public CheckableTreeNode<TagViewModel> TagsRoot
            {
                get
                {
                    //Lazy load, only load the first time
                    if (_tagsRoot != null) return _tagsRoot;
                    
                    //convert to nodes
                    _tagsRoot = new CheckableTreeNode<TagViewModel>(new(new(), _collectionVm), containerOnly: true, propagateChanges: true)
                    {
                        Children = new(_collectionVm.Collection.RootTags
                            .Select(t => new CheckableTreeNode<TagViewModel>(_collectionVm.GetTagVm(t), containerOnly: t.IsGroup, propagateChanges: true)))
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

            public int ImportCount => TagsRoot.Children.Flatten().Count(i => i.IsChecked != false);
        }
    }
}
