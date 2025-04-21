using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using COMPASS.Common.Interfaces;

namespace COMPASS.Common.ViewModels.Modals.Edit
{
    public class CodexBulkEditViewModel : ViewModelBase, IModalViewModel, IConfirmable
    {
        public CodexBulkEditViewModel(List<Codex> toEdit)
        {
            if (toEdit == null || toEdit.Count < 2)
            {
                throw new InvalidOperationException("Bulk edit should only be performed on 2 or more codices");
            }
            
            _editedCodices = toEdit;
            _collection = toEdit.First().Collection;

            //set common metadata
            _commonAuthors = _editedCodices.Select(f => f.Authors.ToList()).Aggregate((xs, ys) => xs.Intersect(ys).ToList());
            _authors = new(_commonAuthors);

            if (_editedCodices.HasCommonValue(f => f.Publisher, out string? commonPublisher))
            {
                _publisher = commonPublisher ?? "";
            }
            
            if (_editedCodices.HasCommonValue(f => f.Rating, out int commonRating))
            {
                _rating = commonRating;
            }
            
            if (_editedCodices.HasCommonValue(f => f.PhysicallyOwned, out bool commonPhysicallyOwned))
            {
                _physicallyOwned = commonPhysicallyOwned;
            }
            
            if (_editedCodices.HasCommonValue(f => f.ReleaseDate, out var commonReleaseDate))
            {
                _releaseDate = commonReleaseDate;
            }
            
            if (_editedCodices.HasCommonValue(f => f.Version, out string? commonVersion))
            {
                _version = commonVersion ?? "";
            }
        }

        private readonly List<Codex> _editedCodices;
        private readonly List<string> _commonAuthors;
        private readonly CodexCollection _collection;
        
        #region Properties

        private ObservableCollection<string> _authors;
        public ObservableCollection<string> Authors
        {
            get => _authors;
            set => SetProperty(ref _authors, value);
        }
        
        private string _publisher = "";
        public string Publisher
        {
            get => _publisher;
            set => SetProperty(ref _publisher, value);
        }

        private int _rating = 0;
        public int Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }
        
        private bool? _physicallyOwned;
        public bool? PhysicallyOwned
        {
            get => _physicallyOwned;
            set => SetProperty(ref _physicallyOwned, value);
        }
        
        private DateTime? _releaseDate;
        public DateTime? ReleaseDate
        {
            get => _releaseDate;
            set => SetProperty(ref _releaseDate, value);
        }
        
        private string _version = "";
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }
        
        private ObservableCollection<TreeNode>? _allTagsAsTreeNodes;
        public ObservableCollection<TreeNode> AllTagsAsTreeNodes => _allTagsAsTreeNodes ??= new(MainViewModel.CollectionVM.CurrentCollection.RootTags.Select(tag => new TreeNode(tag)));

        private ObservableCollection<Tag> _tagsToAdd = [];
        public ObservableCollection<Tag> TagsToAdd
        {
            get => _tagsToAdd;
            set => SetProperty(ref _tagsToAdd, value);
        }

        private ObservableCollection<Tag> _tagsToRemove = [];
        public ObservableCollection<Tag> TagsToRemove
        {
            get => _tagsToRemove;
            set => SetProperty(ref _tagsToRemove, value);
        }
        
        #endregion

        #region Methods and Commands

        private RelayCommand<Tag>? _addTagCommand;
        public RelayCommand<Tag> AddTagCommand => _addTagCommand ??= new(AddTag);

        private void AddTag(Tag? t)
        {
            if (t is null) return;
            if (!TagsToAdd.Contains(t)) TagsToAdd.Add(t);
            else TagsToAdd.Remove(t);
            TagsToRemove.Remove(t);
        }

        private RelayCommand<Tag>? _removeTagCommand;
        public RelayCommand<Tag> RemoveTagCommand => _removeTagCommand ??= new(RemoveTag);

        private void RemoveTag(Tag? t)
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
            foreach (TreeNode node in AllTagsAsTreeNodes.Flatten())
            {
                if (node.Tag != t) continue;
                node.Selected = true;
                node.Selected = false;
                break;
            }
        }

        private RelayCommand? _confirmCommand;
        public IRelayCommand ConfirmCommand => _confirmCommand ??= new(OKBtn);
        public void OKBtn()
        {
            //find added and removed authors
            var deletedAuthors = _commonAuthors.Except(Authors).ToList();
            var addedAuthors = Authors.Except(_commonAuthors).ToList();

            foreach (Codex f in _editedCodices)
            {
                f.Authors = new(f.Authors.Union(addedAuthors).Except(deletedAuthors));
            }

            if (!string.IsNullOrEmpty(Publisher))
            {
                foreach (Codex f in _editedCodices)
                {
                    f.Publisher = Publisher;
                }
            }

            if (PhysicallyOwned != null)
            {
                foreach (Codex f in _editedCodices)
                {
                    f.PhysicallyOwned = PhysicallyOwned.Value;
                }
            }

            if (Rating > 0)
            {
                foreach (Codex f in _editedCodices)
                {
                    f.Rating = Rating;
                }
            }

            if (!string.IsNullOrEmpty(Version))
            {
                foreach (Codex f in _editedCodices)
                {
                    f.Version = Version;
                }
            }

            if (ReleaseDate != null)
            {
                foreach (Codex f in _editedCodices)
                {
                    f.ReleaseDate = ReleaseDate;
                }
            }

            //Update lists of all authors, publishers, ect.
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
            Close();
        }

        private RelayCommand? _cancelCommand;
        public IRelayCommand CancelCommand => _cancelCommand ??= new(Close);
        
        private void Close()
        {
            CloseAction();
        }
        #endregion
        
        
        #region IModalViewModel
        
        public string WindowTitle => "Bulk edit items";
        public int? WindowWidth => 800;
        public int? WindowHeight => 400;
        public Action CloseAction { get; set; } = () => { };

        #endregion

    }
}

