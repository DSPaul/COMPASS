﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using COMPASS.Common.Models;
using COMPASS.Common.Tools;
using System;
using COMPASS.Common.Interfaces;

namespace COMPASS.Common.ViewModels
{
    public class TagEditViewModel : ObservableRecipient, IEditViewModel, IModalViewModel, IRecipient<PropertyChangedMessage<string>>
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
        
        #endregion

        #region Functions and Commands

        private void Clear()
        {
            _editedTag = new();
            TempTag = new(MainViewModel.CollectionVM.CurrentCollection.AllTags);
        }

        /// <inheritdoc/>
        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message is { Sender: Tag, PropertyName: nameof(Tag.Content) })
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
            Clear();
            CloseAction();
        }
        public bool CanOkBtn() => !String.IsNullOrWhiteSpace(TempTag.Content);

        private RelayCommand? _cancelCommand;
        public RelayCommand CancelCommand => _cancelCommand ??= new(Cancel);
        public void Cancel()
        {
            Clear();
            CloseAction();
        }

        private RelayCommand? _colorSameAsParentCommand;
        public RelayCommand ColorSameAsParentCommand => _colorSameAsParentCommand ??= new(SetColorSameAsParent);
        private void SetColorSameAsParent()
        {
            TempTag.InternalBackgroundColor = null;
        }

        #region  IModelViewModel

        public string WindowTitle => CreateNewTag ? "Create new tag" : "Edit tag";

        public int? WindowWidth => null;
        public int? WindowHeight => null;
        
        public Action CloseAction { get; set; } = () => { };

        #endregion

        #endregion
    }
}
