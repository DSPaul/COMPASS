using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
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
        private readonly CollectionManager _collectionManager;

        public ImportTagsViewModel(TagViewModelFactory tagViewModelFactory, CollectionManager collectionManager, string importCollectionId, string targetCollectionId)
            : this(tagViewModelFactory, collectionManager, [importCollectionId], targetCollectionId) { }
        public ImportTagsViewModel(TagViewModelFactory tagViewModelFactory, CollectionManager collectionManager, CollectionHandle importCollectionHandle, string targetCollectionId)
            : this(tagViewModelFactory, collectionManager, [importCollectionHandle], targetCollectionId) { }

        public ImportTagsViewModel(TagViewModelFactory tagViewModelFactory, CollectionManager collectionManager, IEnumerable<string> importCollectionIds, string targetCollectionId) :
            this(tagViewModelFactory, collectionManager, importCollectionIds.Select(id => collectionManager.LoadCollection(id) ?? throw new LoadException(id)), targetCollectionId) { }


        public ImportTagsViewModel(TagViewModelFactory tagViewModelFactory, CollectionManager collectionManager, IEnumerable<CollectionHandle> importCollectionHandles, string targetCollectionId)
        {
            _tagViewModelFactory = tagViewModelFactory;
            _collectionManager = collectionManager;

            //Load the target collection
            _targetCollectionHandle = collectionManager.LoadCollection(targetCollectionId) ?? throw new LoadException(targetCollectionId);

            foreach (var handle in importCollectionHandles)
            {
                _collectionHandles.Add(handle);
            }

            TagsSelectorVM = new TagsSelectorViewModel(tagViewModelFactory, _collectionHandles.Select(h => h.CollectionVM));
        }

        public ImportTagsViewModel(TagViewModelFactory tagViewModelFactory, CollectionManager collectionManager, CodexCollectionVM codexCollectionVM, string targetCollectionId)
        {
            _tagViewModelFactory = tagViewModelFactory;
            _collectionManager = collectionManager;

            //Load the target collection
            _targetCollectionHandle = collectionManager.LoadCollection(targetCollectionId) ?? throw new LoadException(targetCollectionId);
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

    [Factory]
    public class ImportTagsViewModelFactory(TagViewModelFactory tagViewModelFactory, CollectionManager collectionManager)
    {
        public ImportTagsViewModel Create(IEnumerable<string> importCollectionIds, string targetCollectionId)
            => new(tagViewModelFactory, collectionManager, importCollectionIds, targetCollectionId);

        public ImportTagsViewModel Create(CodexCollectionVM codexCollectionVM, string targetCollectionId)
            => new(tagViewModelFactory, collectionManager, codexCollectionVM, targetCollectionId);
    }
}
