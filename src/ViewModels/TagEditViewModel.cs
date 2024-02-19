using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using System;

namespace COMPASS.ViewModels
{
    public class TagEditViewModel : ObservableObject, IEditViewModel
    {
        public TagEditViewModel(Tag? toEdit, bool createNew)
        {
            _editedTag = toEdit ?? new(MainViewModel.CollectionVM.CurrentCollection.AllTags);
            CreateNewTag = createNew;

            _tempTag = new Tag(_editedTag);
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
                RaisePropertyChanged(nameof(ShowInfoGrid));
            }
        }

        //visibility of General Info Selection
        public bool ShowInfoGrid => !ShowColorSelection;

        #endregion

        #region Functions and Commands

        private ActionCommand? _oKCommand;
        public ActionCommand OKCommand => _oKCommand ??= new(OKBtn, CanOkBtn);
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

        private ActionCommand? _cancelCommand;
        public ActionCommand CancelCommand => _cancelCommand ??= new(Cancel);
        public void Cancel()
        {
            if (CreateNewTag)
            {
                TempTag = new Tag(MainViewModel.CollectionVM.CurrentCollection.AllTags);
            }
            _editedTag = null;
            CloseAction();
        }

        private ActionCommand? _closeColorSelectionCommand;
        public ActionCommand CloseColorSelectionCommand => _closeColorSelectionCommand ??= new(CloseColorSelection);

        private ActionCommand? _colorSameAsParentCommand;
        public ActionCommand ColorSameAsParentCommand => _colorSameAsParentCommand ??= new(SetColorSameAsParent);
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
