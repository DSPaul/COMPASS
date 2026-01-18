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

        public ImportTagsViewModel(IEnumerable<string> importCollectionIds, string targetCollectionId)
        {
            WindowTitle = "Import Tags";

            //Load all the collections
            _targetCollectionHandle = CollectionManager.LoadCollection(targetCollectionId) ?? throw new LoadException(targetCollectionId);

            //TODO optimize so only tags get loaded
            List<CodexCollectionVM> collectionVms = [];
            foreach (var collectionId in importCollectionIds)
            {
                var handle = CollectionManager.LoadCollection(collectionId);
                if (handle != null)
                {
                    _collectionHandles.Add(handle);
                    collectionVms.Add(handle.CollectionVM);
                }
            }
            
            TagsSelectorVM = new TagsSelectorViewModel(collectionVms);
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

        public string WindowTitle { get; }
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
