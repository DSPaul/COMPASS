using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Common.ViewModels.Selection;

namespace COMPASS.Common.ViewModels.Modals.Import
{
    public class ImportTagsViewModel : ViewModelBase, IDisposable, IModalViewModel, IConfirmable
    {
        private readonly TagViewModelFactory _tagViewModelFactory;

        public ImportTagsViewModel(TagViewModelFactory tagViewModelFactory, string importCollectionId, string targetCollectionId)
            : this(tagViewModelFactory, [importCollectionId], targetCollectionId) { }
        public ImportTagsViewModel(TagViewModelFactory tagViewModelFactory, CollectionHandle importCollectionHandle, string targetCollectionId)
            : this(tagViewModelFactory, [importCollectionHandle], targetCollectionId) { }

        public ImportTagsViewModel(TagViewModelFactory tagViewModelFactory, IEnumerable<string> importCollectionIds, string targetCollectionId) :
            this(tagViewModelFactory, importCollectionIds.Select(id => CollectionManager.LoadCollection(id) ?? throw new LoadException(id)), targetCollectionId) { }


        public ImportTagsViewModel(TagViewModelFactory tagViewModelFactory, IEnumerable<CollectionHandle> importCollectionHandles, string targetCollectionId)
        {
            _tagViewModelFactory = tagViewModelFactory;

            //Load the target collection
            _targetCollectionHandle = CollectionManager.LoadCollection(targetCollectionId) ?? throw new LoadException(targetCollectionId);

            foreach (var handle in importCollectionHandles)
            {
                _collectionHandles.Add(handle);
            }

            TagsSelectorVM = new TagsSelectorViewModel(tagViewModelFactory, _collectionHandles.Select(h => h.CollectionVM));
        }

        public ImportTagsViewModel(TagViewModelFactory tagViewModelFactory, CodexCollectionVM codexCollectionVM, string targetCollectionId)
        {
            _tagViewModelFactory = tagViewModelFactory;

            //Load the target collection
            _targetCollectionHandle = CollectionManager.LoadCollection(targetCollectionId) ?? throw new LoadException(targetCollectionId);
            TagsSelectorVM = new TagsSelectorViewModel(tagViewModelFactory, codexCollectionVM);
        }

        private readonly IList<CollectionHandle> _collectionHandles = [];
        private readonly CollectionHandle _targetCollectionHandle;
        
        public TagsSelectorViewModel TagsSelectorVM { get; set; }

        private RelayCommand? _confirmCommand;
        public IRelayCommand ConfirmCommand => _confirmCommand ??= new(ImportTags);

        private RelayCommand? _cancelCommand;
        public IRelayCommand CancelCommand => _cancelCommand ??= new(CloseAction);

        private void ImportTags()
        {
            foreach (var template in TagsSelectorVM.TagCollections)
            {
                var selectedTags = CheckableTreeNode.GetCheckedModels<TagViewModel, Tag>(template.TagsRoot.Children).ToList();

                if (selectedTags.Any())
                {
                    _targetCollectionHandle.CollectionVM.Collection.AddTags(selectedTags);
                }
            }
            CloseAction.Invoke();
        }

        public string WindowTitle { get; } = "Import Tags";
        public Action CloseAction { get; set; } = () => { };

        public void Dispose()
        {
            foreach (var collectionHandle in _collectionHandles)
            {
                collectionHandle.Dispose();
            }
            _targetCollectionHandle.Dispose();
        }

    }
}
