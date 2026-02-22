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
    public class ImportTagsViewModel : ViewModelBase, IDisposable, IModalViewModel
    {
        public ImportTagsViewModel(string importCollectionId, string targetCollectionId) : this([importCollectionId], targetCollectionId) { }
        public ImportTagsViewModel(CollectionHandle importCollectionHandle, string targetCollectionId) : this([importCollectionHandle], targetCollectionId) { }

        public ImportTagsViewModel(IEnumerable<string> importCollectionIds, string targetCollectionId) :
            this(importCollectionIds.Select(id => CollectionManager.LoadCollection(id) ?? throw new LoadException(id)), targetCollectionId) { }


        public ImportTagsViewModel(IEnumerable<CollectionHandle> importCollectionHandles, string targetCollectionId)
        {
            //Load the target collection
            _targetCollectionHandle = CollectionManager.LoadCollection(targetCollectionId) ?? throw new LoadException(targetCollectionId);

            foreach (var handle in importCollectionHandles)
            {
                _collectionHandles.Add(handle);
            }

            TagsSelectorVM = new TagsSelectorViewModel(_collectionHandles.Select(h => h.CollectionVM));
        }

        public ImportTagsViewModel(CodexCollectionVM codexCollectionVM, string targetCollectionId)
        {
            //Load the target collection
            _targetCollectionHandle = CollectionManager.LoadCollection(targetCollectionId) ?? throw new LoadException(targetCollectionId);
            TagsSelectorVM = new TagsSelectorViewModel(codexCollectionVM);
        }

        private readonly IList<CollectionHandle> _collectionHandles = [];
        private readonly CollectionHandle _targetCollectionHandle;
        
        public TagsSelectorViewModel TagsSelectorVM { get; set; }

        private RelayCommand? _importTagsCommand;
        public RelayCommand ImportTagsCommand => _importTagsCommand ??= new(ImportTags);

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
