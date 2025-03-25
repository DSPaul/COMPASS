using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Tools;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using COMPASS.Common.Interfaces;

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

            PossibleParents  = new(MainViewModel.CollectionVM.CurrentCollection.RootTags.Select(tag => new TreeNode(tag)
            {
                Expanded = tag.Children.Flatten().Contains(_tempTag.Parent) //expand all parents so that parent is visible
            }));
        }

        private void HandleTagPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tag.Content))
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

        public ObservableCollection<TreeNode> PossibleParents { get; }

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

        #endregion
        
        #region IConfirmable
        
        private RelayCommand? _confirmCommand;
        public RelayCommand ConfirmCommand => _confirmCommand ??= new(Confirm, CanConfirm);
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
            
            MainViewModel.CollectionVM.CurrentCollection.SaveTags();

            MainViewModel.CollectionVM.TagsVM.BuildTagTreeView();

            //reset fields
            Clear();
            CloseAction();
        }
        public bool CanConfirm() => !string.IsNullOrWhiteSpace(TempTag.Content);

        private RelayCommand? _cancelCommand;
        public RelayCommand CancelCommand => _cancelCommand ??= new(Cancel);
        public void Cancel()
        {
            Clear();
            CloseAction();
        }
        #endregion
        
        #region  IModelViewModel

        public string WindowTitle => CreateNewTag ? "Create new tag" : "Edit tag";

        public int? WindowWidth => null;
        public int? WindowHeight => null;
        
        public Action CloseAction { get; set; } = () => { };

        #endregion
    }
}
