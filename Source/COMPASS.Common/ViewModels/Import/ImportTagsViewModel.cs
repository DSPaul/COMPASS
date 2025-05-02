﻿using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using COMPASS.Common.Models.Hierarchy;

namespace COMPASS.Common.ViewModels.Import
{
    public class ImportTagsViewModel : ViewModelBase
    {
        public ImportTagsViewModel(CodexCollection collection) : this([collection]) { }

        public ImportTagsViewModel(List<CodexCollection> collections)
        {
            TagsSelectorVM = new TagsSelectorViewModel(collections);
        }

        public TagsSelectorViewModel TagsSelectorVM { get; set; }

        private RelayCommand? _importTagsCommand;
        public RelayCommand ImportTagsCommand => _importTagsCommand ??= new(ImportTags);
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
