using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.ViewModels.Modals.Edit
{
    public class CodexBulkEditViewModel : ViewModelBase, IConfirmable, IModalViewModel
    {
        public CodexBulkEditViewModel(List<Codex> toEdit, CollectionTabVM? tabVm = null)
        {
            if (toEdit == null || toEdit.Count < 2)
            {
                throw new InvalidOperationException("Bulk edit should only be performed on 2 or more codices");
            }
         
            TabVM = tabVm ?? TabsViewModel.GetInstance().ActiveTab ?? throw new NoTabException("An active tab is expected when editing a codex");
        
            var publisherList = TabVM.FiltersVM.PublisherList;
            PublisherOptions = ["", ..publisherList];
            
            _editedCodices = toEdit;
            AllTagsAsTreeNodes = BuildTagTree();

            AuthorsToRemoveOptions = _editedCodices
                .SelectMany(codex => codex.Authors)
                .Where(author => !string.IsNullOrWhiteSpace(author))
                .Distinct()
                .OrderBy(author => author)
                .ToList();

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

        #region Properties

        private ObservableCollection<string> _authorsToAdd = [];
        public ObservableCollection<string> AuthorsToAdd
        {
            get => _authorsToAdd;
            set => SetProperty(ref _authorsToAdd, value);
        }

        private ObservableCollection<string> _authorsToRemove = [];
        public ObservableCollection<string> AuthorsToRemove
        {
            get => _authorsToRemove;
            set => SetProperty(ref _authorsToRemove, value);
        }

        public List<string> AuthorsToRemoveOptions { get; }

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
        
        private ObservableCollection<TagViewModel> _tagsToAdd = [];
        public ObservableCollection<TagViewModel> TagsToAdd
        {
            get => _tagsToAdd;
            set => SetProperty(ref _tagsToAdd, value);
        }

        private ObservableCollection<TagViewModel> _tagsToRemove = [];
        public ObservableCollection<TagViewModel> TagsToRemove
        {
            get => _tagsToRemove;
            set => SetProperty(ref _tagsToRemove, value);
        }
        
        public CollectionTabVM TabVM { get; }
    
        public ObservableCollection<TreeNode<TagViewModel>> AllTagsAsTreeNodes { get; }
    
        public List<string> PublisherOptions { get; }
        
        #endregion

        #region Methods and Commands

        private RelayCommand<TagViewModel>? _addTagCommand;
        public RelayCommand<TagViewModel> AddTagCommand => _addTagCommand ??= new(AddTag);

        private void AddTag(TagViewModel? t)
        {
            if (t is null) return;
            if (!TagsToAdd.Contains(t)) TagsToAdd.Add(t);
            else TagsToAdd.Remove(t);
            TagsToRemove.Remove(t);
        }

        private RelayCommand<TagViewModel>? _removeTagCommand;
        public RelayCommand<TagViewModel> RemoveTagCommand => _removeTagCommand ??= new(RemoveTag);

        private void RemoveTag(TagViewModel? t)
        {
            if (t is null) return;
            if (!TagsToRemove.Contains(t)) TagsToRemove.Add(t);
            else TagsToRemove.Remove(t);
            TagsToAdd.Remove(t);
        }

        private RelayCommand<TagViewModel>? _removeFromItemsControlCommand;
        public RelayCommand<TagViewModel> RemoveFromItemsControlCommand => _removeFromItemsControlCommand ??= new(RemoveTagFromItemsControl);
        private void RemoveTagFromItemsControl(TagViewModel? t)
        {
            if (t is null) return;
            TagsToAdd.Remove(t);
            TagsToRemove.Remove(t);
        }

        private ObservableCollection<TreeNode<TagViewModel>> BuildTagTree()
        {
            ObservableCollection<TreeNode<TagViewModel>> roots = new(TabVM.CollectionVM.Collection.RootTags
                .Select(TabVM.CollectionVM.GetTagVm)
                .Select(tagVm => new TreeNode<TagViewModel>(tagVm)));

            foreach (var node in roots.Flatten())
            {
                node.Expanded = node.Item.IsGroup;
            }

            return roots;
        }

        #endregion

        #region IConfirmable

        private RelayCommand? _confirmCommand;
        public IRelayCommand ConfirmCommand => _confirmCommand ??= new(Confirm);
        private void Confirm()
        {
            if (AuthorsToAdd.Count > 0 || AuthorsToRemove.Count > 0)
            {
                foreach (Codex f in _editedCodices)
                {
                    f.Authors = new(f.Authors.Union(AuthorsToAdd).Except(AuthorsToRemove));
                }
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

            //Add and remove Tags
            foreach (Codex codex in _editedCodices)
            {
                if (TagsToAdd.Count > 0)
                {
                    //add all tags from TagsToAdd
                    foreach (TagViewModel t in TagsToAdd) codex.Tags.AddIfMissing(t.GetModel());
                }
                if (TagsToRemove.Count > 0)
                {
                    //remove Tags from TagsToRemove
                    foreach (TagViewModel t in TagsToRemove) codex.Tags.Remove(t.GetModel());
                }
            }
            CloseAction();
        }
    
        private RelayCommand? _cancelCommand;
        public IRelayCommand CancelCommand => _cancelCommand ??= new(Cancel);

        protected virtual void Cancel()
        {
            CloseAction();
        }

        #endregion

        #region IModalWindow
    
        public string WindowTitle => "Bulk edit items";
        public Action CloseAction { get; set; } = () => { };

        #endregion

    }
}

