﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using COMPASS.Models;
using COMPASS.Tools;
using System;

namespace COMPASS.ViewModels
{
    public class TagEditViewModel : ObservableRecipient, IEditViewModel, IRecipient<PropertyChangedMessage<string>>
    {
        public TagEditViewModel(Tag? toEdit, bool createNew) : base()
        {
            _editedTag = toEdit ?? new(MainViewModel.CollectionVM.CurrentCollection.AllTags);
            CreateNewTag = createNew;

            _tempTag = new Tag(_editedTag);

            IsActive = true;
        }

        #region Properties

        private Tag _editedTag;
        public bool CreateNewTag { get; init; }

        //TempTag to work with
        private Tag _tempTag;
        public Tag TempTag
        {
            get => _tempTag;
            set => SetProperty(ref _tempTag, value);
        }

        //visibility of Color Selection
        private bool _showColorSelection = false;
        public bool ShowColorSelection
        {
            get => _showColorSelection;
            set
            {
                SetProperty(ref _showColorSelection, value);
                OnPropertyChanged(nameof(ShowInfoGrid));
            }
        }

        //visibility of General Info Selection
        public bool ShowInfoGrid => !ShowColorSelection;

        #endregion

        #region Functions and Commands

        /// <inheritdoc/>
        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender.GetType() == typeof(Tag) && message.PropertyName == nameof(Tag.Content))
            {
                OKCommand.NotifyCanExecuteChanged();
            }
        }

        private RelayCommand? _oKCommand;
        public RelayCommand OKCommand => _oKCommand ??= new(OKBtn, CanOkBtn);
        public void OKBtn()
        {
            //Apply changes 
            _editedTag.Copy(TempTag);

            if (CreateNewTag)
            {
                if (TempTag.Parent is null)
                {
                    MainViewModel.CollectionVM.CurrentCollection.RootTags.Add(_editedTag);
                }
                else
                {
                    TempTag.Parent.Children.Add(_editedTag);
                }

                _editedTag.ID = Utils.GetAvailableID(MainViewModel.CollectionVM.CurrentCollection.AllTags);
                MainViewModel.CollectionVM.CurrentCollection.AllTags.Add(_editedTag);
            }

            MainViewModel.CollectionVM.CurrentCollection.SaveTags();

            MainViewModel.CollectionVM.TagsVM.BuildTagTreeView();

            //reset fields
            TempTag = new(MainViewModel.CollectionVM.CurrentCollection.AllTags);
            _editedTag = new();
            CloseAction();
        }
        public bool CanOkBtn() => !String.IsNullOrWhiteSpace(TempTag.Content);

        private RelayCommand? _cancelCommand;
        public RelayCommand CancelCommand => _cancelCommand ??= new(Cancel);
        public void Cancel()
        {
            if (CreateNewTag)
            {
                TempTag = new Tag(MainViewModel.CollectionVM.CurrentCollection.AllTags);
            }
            _editedTag = new(MainViewModel.CollectionVM.CurrentCollection.AllTags);
            CloseAction();
        }

        private RelayCommand? _closeColorSelectionCommand;
        public RelayCommand CloseColorSelectionCommand => _closeColorSelectionCommand ??= new(CloseColorSelection);

        private RelayCommand? _colorSameAsParentCommand;
        public RelayCommand ColorSameAsParentCommand => _colorSameAsParentCommand ??= new(SetColorSameAsParent);
        private void SetColorSameAsParent()
        {
            TempTag.SerializableBackgroundColor = null;
            CloseColorSelection();
        }

        private void CloseColorSelection() => ShowColorSelection = false;
        public Action CloseAction { get; set; } = () => { };

        #endregion
    }
}
