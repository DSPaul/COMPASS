using COMPASS.Commands;
using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace COMPASS.ViewModels.Import
{
    public class ImportTagsViewModel : ViewModelBase_avalonia
    {
        public ImportTagsViewModel(CodexCollection collection) : this(new List<CodexCollection> { collection }) { }

        public ImportTagsViewModel(List<CodexCollection> collections)
        {
            TagsSelectorVM = new TagsSelectorViewModel(collections);
        }

        public TagsSelectorViewModel TagsSelectorVM { get; set; }

        private ActionCommand? _importTagsCommand;
        public ActionCommand ImportTagsCommand => _importTagsCommand ??= new(ImportTags);
        public void ImportTags()
        {
            foreach (var template in TagsSelectorVM.TagCollections)
            {
                var tags = CheckableTreeNode<Tag>.GetCheckedItems(template.TagsRoot.Children).ToList();
                if (tags.Any())
                {
                    MainViewModel.CollectionVM.CurrentCollection.AddTags(tags);
                }
            }
            CloseAction.Invoke();
        }

        public Action CloseAction = () => { };
    }
}
