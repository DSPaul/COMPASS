using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Resources.Controls.MultiSelectCombobox;
using COMPASS.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.ViewModels
{
    public class CodexBulkEditViewModel : ViewModelBase_avalonia, IEditViewModel
    {
        public CodexBulkEditViewModel(List<Codex> toEdit)
        {
            _editedCodices = toEdit;
            _tempCodex = new();

            //set common metadata
            _commonAuthors = _editedCodices.Select(f => f.Authors.ToList()).Aggregate((xs, ys) => xs.Intersect(ys).ToList());
            TempCodex.Authors = new(_commonAuthors);
            if (_editedCodices.All(f => f.Publisher == _editedCodices[0].Publisher))
                TempCodex.Publisher = _editedCodices[0].Publisher;
            if (_editedCodices.All(f => f.Rating == _editedCodices[0].Rating))
                TempCodex.Rating = _editedCodices[0].Rating;
            if (_editedCodices.All(f => f.PhysicallyOwned == _editedCodices[0].PhysicallyOwned))
                TempCodex.PhysicallyOwned = _editedCodices[0].PhysicallyOwned;
            if (_editedCodices.All(f => f.ReleaseDate == _editedCodices[0].ReleaseDate))
                TempCodex.ReleaseDate = _editedCodices[0].ReleaseDate;
        }

        private readonly List<Codex> _editedCodices;
        private readonly List<string> _commonAuthors;

        #region Properties

        private ObservableCollection<TreeViewNode>? _treeViewSource;
        public ObservableCollection<TreeViewNode> TreeViewSource => _treeViewSource ??= new(MainViewModel.CollectionVM.CurrentCollection.RootTags.Select(tag => new TreeViewNode(tag)));


        private ObservableCollection<Tag> _tagsToAdd = new();
        public ObservableCollection<Tag> TagsToAdd
        {
            get => _tagsToAdd;
            set => SetProperty(ref _tagsToAdd, value);
        }

        private ObservableCollection<Tag> _tagsToRemove = new();
        public ObservableCollection<Tag> TagsToRemove
        {
            get => _tagsToRemove;
            set => SetProperty(ref _tagsToRemove, value);
        }

        private Codex _tempCodex;
        public Codex TempCodex
        {
            get => _tempCodex;
            set => SetProperty(ref _tempCodex, value);
        }

        public CreatableLookUpContract Contract { get; set; } = new();

        #endregion

        #region Methods and Commands

        private RelayCommand<Tag>? _plusCheckCommand;
        public RelayCommand<Tag> PlusCheckCommand => _plusCheckCommand ??= new(AddTag);

        public void AddTag(Tag? t)
        {
            if (t is null) return;
            if (!TagsToAdd.Contains(t)) TagsToAdd.Add(t);
            else TagsToAdd.Remove(t);
            TagsToRemove.Remove(t);
        }

        private RelayCommand<Tag>? _minCheckCommand;
        public RelayCommand<Tag> MinCheckCommand => _minCheckCommand ??= new(RemoveTag);

        public void RemoveTag(Tag? t)
        {
            if (t is null) return;
            if (!TagsToRemove.Contains(t)) TagsToRemove.Add(t);
            else TagsToRemove.Remove(t);
            TagsToAdd.Remove(t);
        }

        private RelayCommand<Tag>? _removeFromItemsControlCommand;
        public RelayCommand<Tag> RemoveFromItemsControlCommand => _removeFromItemsControlCommand ??= new(RemoveTagFromItemsControl);
        private void RemoveTagFromItemsControl(Tag? t)
        {
            if (t is null) return;
            TagsToAdd.Remove(t);
            TagsToRemove.Remove(t);
            foreach (var node in TreeViewSource.Flatten())
            {
                if (node.Tag == t)
                {
                    node.Selected = true;
                    node.Selected = false;
                    break;
                }
            }
        }

        public Action CloseAction { get; set; } = () => { };

        private ActionCommand? _okCommand;
        public ActionCommand OKCommand => _okCommand ??= new(OKBtn);
        public void OKBtn()
        {
            //Copy changes into each Codex

            //find added and removed authors
            var deletedAuthors = _commonAuthors.Except(TempCodex.Authors).ToList();
            var addedAuthors = TempCodex.Authors.Except(_commonAuthors).ToList();

            foreach (Codex f in _editedCodices)
            {
                f.Authors = new(f.Authors.Union(addedAuthors).Except(deletedAuthors));
            }

            if (!String.IsNullOrEmpty(TempCodex.Publisher))
            {
                foreach (Codex f in _editedCodices)
                {
                    f.Publisher = TempCodex.Publisher;
                }
            }

            if (TempCodex.PhysicallyOwned)
            {
                foreach (Codex f in _editedCodices)
                {
                    f.PhysicallyOwned = true;
                }
            }

            if (TempCodex.Rating > 0)
            {
                foreach (Codex f in _editedCodices)
                {
                    f.Rating = TempCodex.Rating;
                }
            }

            if (TempCodex.Version != null)
            {
                foreach (Codex f in _editedCodices)
                {
                    f.Version = TempCodex.Version;
                }
            }

            if (TempCodex.ReleaseDate != null)
            {
                foreach (Codex f in _editedCodices)
                {
                    f.ReleaseDate = TempCodex.ReleaseDate;
                }
            }

            //Update list of all authors, publishers, ect.
            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();

            //Add and remove Tags
            foreach (Codex f in _editedCodices)
            {
                if (TagsToAdd.Count > 0)
                {
                    //add all tags from TagsToAdd
                    foreach (Tag t in TagsToAdd) f.Tags.Add(t);
                    //remove duplicates
                    f.Tags = new(f.Tags.Distinct());
                }
                if (TagsToRemove.Count > 0)
                {
                    //remove Tags from TagsToRemove
                    foreach (Tag t in TagsToRemove) f.Tags.Remove(t);
                }
            }
            CloseAction();
        }

        private ActionCommand? _cancelCommand;
        public ActionCommand CancelCommand => _cancelCommand ??= new(Cancel);
        public void Cancel() => CloseAction();

        #endregion
    }
}

