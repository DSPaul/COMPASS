using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Selection;

namespace COMPASS.Common.ViewModels.Modals.Import
{
    public class ImportTagsViewModel : ViewModelBase, IDisposable, IModalViewModel
    {
        public ImportTagsViewModel(string collectionId) : this([collectionId]) { }

        public ImportTagsViewModel(IEnumerable<string> collectionIds)
        {
            WindowTitle = "Import Tags";

            //Load all the collections
            //TODO optimize so only tags get loaded
            List<CodexCollection> collections = new List<CodexCollection>();
            foreach (var collectionId in collectionIds)
            {
                var handle = CollectionManager.LoadCollection(collectionId);
                if (handle != null)
                {
                    _collectionHandles.Add(handle);
                    collections.Add(handle.CollectionVM.Collection);
                }
            }
            
            TagsSelectorVM = new TagsSelectorViewModel(collections);
        }

        private readonly IList<CollectionHandle> _collectionHandles = [];
        
        public TagsSelectorViewModel TagsSelectorVM { get; set; }

        private RelayCommand? _importTagsCommand;
        public RelayCommand ImportTagsCommand => _importTagsCommand ??= new(ImportTags);

        private void ImportTags()
        {
            foreach (var template in TagsSelectorVM.TagCollections)
            {
                var selectedTags = CheckableTreeNode<Tag>.GetCheckedItems(template.TagsRoot.Children).ToList();
                if (selectedTags.Any())
                {
                    //TODO might make the target collection a dropdown later on
                    ActiveCollection.AddTags(selectedTags);
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
        }

    }
}
