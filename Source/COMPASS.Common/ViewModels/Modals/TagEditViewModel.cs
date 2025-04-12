using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.FileSystem;
using Notification = COMPASS.Common.Models.Notification;

namespace COMPASS.Common.ViewModels.Modals
{
    public class TagEditViewModel : ViewModelBase, IConfirmable, IModalViewModel
    {
        public TagEditViewModel(Tag? toEdit, bool createNew) : base()
        {
            _editedTag = toEdit ?? new(MainViewModel.CollectionVM.CurrentCollection.AllTags);
            CreateNewTag = createNew;

            _tempTag = new Tag(_editedTag);
            _tempTag.PropertyChanged += HandleTagPropertyChanged;

            _possibleParents = GetPossibleParents();
        }

        private void HandleTagPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tag.Name))
            {
                ConfirmCommand.NotifyCanExecuteChanged();
            }
        }
        
        #region Properties

        private Tag _editedTag;
        public bool CreateNewTag { get; init; }

        //TempTag to work with
        private Tag _tempTag;
        public Tag TempTag
        {
            get => _tempTag;
            set
            {
                if (_tempTag != null)
                {
                    _tempTag.PropertyChanged -= HandleTagPropertyChanged;
                }

                if (SetProperty(ref _tempTag, value))
                {
                    ConfirmCommand.NotifyCanExecuteChanged();
                }
                if (_tempTag != null)
                {
                    _tempTag.PropertyChanged += HandleTagPropertyChanged;
                }
            }
        }

        private ObservableCollection<TreeNode> _possibleParents;

        public ObservableCollection<TreeNode> PossibleParents
        {
            get => _possibleParents;
            set
            {
                if (SetProperty(ref _possibleParents, value))
                {
                    OnPropertyChanged(nameof(HasPossibleParents));
                }
            }
        }

        public bool HasPossibleParents => PossibleParents.Any(node => node.Tag != _editedTag);

        public TreeNode? SelectedParent
        {
            get => PossibleParents.Flatten().FirstOrDefault(node => node.Tag == TempTag.Parent);
            set => TempTag.Parent = value?.Tag;
        }
        
        #endregion

        #region Methods and Commands

        private void Clear()
        {
            _editedTag = new();
            TempTag = new(MainViewModel.CollectionVM.CurrentCollection.AllTags);

            //reset parents as new tag might have just been added
            PossibleParents = GetPossibleParents();
        }

        private ObservableCollection<TreeNode> GetPossibleParents()
        {
            var collection = MainViewModel.CollectionVM.CurrentCollection.RootTags.Select(tag => new TreeNode(tag)).ToList();

            foreach (TreeNode node in collection.Flatten())
            {
                node.Expanded = node.Tag.Children.Flatten().Contains(_tempTag.Parent); //expand all parents so that parent is visible
            } 
            
            return new(collection);
        }
        
        private RelayCommand? _colorSameAsParentCommand;
        public RelayCommand ColorSameAsParentCommand => _colorSameAsParentCommand ??= new(SetColorSameAsParent);
        private void SetColorSameAsParent()
        {
            TempTag.InternalBackgroundColor = null;
        }
        
        private RelayCommand? _clearParentCommand;
        public RelayCommand ClearParentCommand => _clearParentCommand ??= new(ClearParent);
        private void ClearParent()
        {
            TempTag.Parent = null;
        }
        
        private RelayCommand? _detectLinksCommand;
        public RelayCommand DetectLinksCommand => _detectLinksCommand ??= new(DetectLinks, CanDetectLinks);
        
        private void DetectLinks()
        {
            var relevantCodices = MainViewModel.CollectionVM.CurrentCollection.AllCodices
                .Where(codex => codex.Sources.HasOfflineSource() && 
                                codex.Tags.Contains(_editedTag))
                .ToList();
            
            var splitFolders = relevantCodices.Select(codex => codex.Sources.Path)
                                              .SelectMany(path => path.Split("\\"))
                                              .ToHashSet();
        
            foreach (string folder in splitFolders)
            {
                var codicesInFolder = MainViewModel.CollectionVM.CurrentCollection.AllCodices
                    .Where(codex => codex.Sources.HasOfflineSource())
                    .Where(codex => codex.Sources.Path.Contains(@"\" + folder + @"\"))
                    .ToList();
                
                if (codicesInFolder.Count < 3) continue;  //Require at least 3 codices in same folder before we can speak of a pattern

                string glob = $"**/{folder}/**";
                
                if (codicesInFolder.All(codx => codx.Tags.Contains(_editedTag)) &&
                    !TempTag.CalculatedLinkedGlobs.Contains(glob))
                {
                    TempTag.LinkedGlobs.AddIfMissing(glob);
                }
            }
        }
        
        private bool CanDetectLinks() => !CreateNewTag;
        
        private AsyncRelayCommand? _applyLinksCommand;
        public AsyncRelayCommand ApplyLinksCommand => _applyLinksCommand ??= new(ApplyLinks, CanApplyChanges);

        private async Task ApplyLinks()
        {
            var globs = TempTag.LinkedGlobs.Concat(TempTag.CalculatedLinkedGlobs).ToList();
            List<Codex> matchingCodices = MainViewModel.CollectionVM.CurrentCollection.AllCodices
                .Where(codex => IOService.MatchesAnyGlob(codex.Sources.Path, globs) &&
                                !codex.Tags.Contains(_editedTag))
                .ToList();

            Notification notification;

            if (matchingCodices.Any())
            {
                notification = new(
                    $"{matchingCodices.Count} matching items found",
                    $"This tag will be added to {matchingCodices.Count} items")
                {
                    Details = string.Join('\n', matchingCodices.Select(codex => codex.Title)),
                    Actions = NotificationAction.Confirm | NotificationAction.Cancel,
                };
            }
            else
            {
                notification =new(
                    $"No new matching items found",
                    $"Either no matches were found or all matching items already contain this tag.");
            }
            
            await App.Container.Resolve<INotificationService>().ShowDialog(notification);

            if (notification.Result == NotificationAction.Confirm)
            {
                foreach (Codex codex in matchingCodices)
                {
                    codex.Tags.Add(_editedTag);
                }
            }
        }

        private bool CanApplyChanges() => !CreateNewTag;

        #endregion
        
        #region IConfirmable
        
        private RelayCommand? _confirmCommand;
        public IRelayCommand ConfirmCommand => _confirmCommand ??= new(Confirm, CanConfirm);
        public void Confirm()
        {
            //Apply changes 
            Tag? oldParent = _editedTag.Parent;
            _editedTag.CopyFrom(TempTag);

            if (CreateNewTag)
            {
                _editedTag.ID = Utils.GetAvailableID(MainViewModel.CollectionVM.CurrentCollection.AllTags);
                MainViewModel.CollectionVM.CurrentCollection.AllTags.Add(_editedTag);
            }

            if (CreateNewTag || oldParent != _editedTag.Parent)
            {
                //remove from old parent
                if (oldParent == null)
                {
                    MainViewModel.CollectionVM.CurrentCollection.RootTags.Remove(_editedTag);
                }
                else
                {
                    oldParent.Children.Remove(_editedTag);
                }
                
                //Add to new parent
                if (_editedTag.Parent == null)
                {
                    MainViewModel.CollectionVM.CurrentCollection.RootTags.Add(_editedTag);
                }
                else
                {
                    _editedTag.Parent.Children.Add(_editedTag);
                }

            }
            
            var collectionStorageService = App.Container.Resolve<ICodexCollectionStorageService>();
            collectionStorageService.SaveTags(MainViewModel.CollectionVM.CurrentCollection);

            MainViewModel.CollectionVM.TagsVM.BuildTagTreeView();

            //reset fields
            Clear();
            CloseAction();
        }
        public bool CanConfirm() => !string.IsNullOrWhiteSpace(TempTag.Name);

        private RelayCommand? _cancelCommand;
        public IRelayCommand CancelCommand => _cancelCommand ??= new(Cancel);
        public void Cancel()
        {
            Clear();
            CloseAction();
        }
        #endregion
        
        #region  IModalViewModel

        public string WindowTitle => CreateNewTag ? "Create new tag" : "Edit tag";

        public int? WindowWidth => null;
        public int? WindowHeight => null;
        
        public Action CloseAction { get; set; } = () => { };

        #endregion
    }
}
