using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace COMPASS.ViewModels.Import
{
    public class ImportTagsViewModel : ViewModelBase
    {
        public ImportTagsViewModel(List<CodexCollection> collections)
        {
            TagTemplates = collections.Select(c => new TagTemplateHelper(c)).ToList();
            SelectedTagTemplate = TagTemplates.FirstOrDefault();
        }

        private List<TagTemplateHelper> _tagTemplates = new();
        public List<TagTemplateHelper> TagTemplates
        {
            get => _tagTemplates;
            set => SetProperty(ref _tagTemplates, value);
        }

        private TagTemplateHelper _selectedTagTemplate;
        public TagTemplateHelper SelectedTagTemplate
        {
            get => _selectedTagTemplate;
            set => SetProperty(ref _selectedTagTemplate, value);
        }

        private ActionCommand _mergeTagsCommand;
        public ActionCommand MergeTagsCommand => _mergeTagsCommand ??= new(MergeTags);
        public void MergeTags()
        {
            foreach (var template in TagTemplates)
            {
                var tags = CheckableTreeNode<Tag>.GetCheckedItems(template.TagsRoot.Children);
                if (tags.Any())
                {
                    MainViewModel.CollectionVM.CurrentCollection.ImportTags(tags);
                }
            }
            CloseAction.Invoke();
        }

        public Action CloseAction = () => { };

        public class TagTemplateHelper : ObservableObject
        {
            public TagTemplateHelper(CodexCollection c)
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
