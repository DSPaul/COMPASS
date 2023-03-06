using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using System;

namespace COMPASS.ViewModels
{
    public class TagEditViewModel : ObservableObject, IEditViewModel
    {
        public TagEditViewModel(Tag toEdit, bool createNew) : base()
        {
            EditedTag = toEdit ?? new();
            CreateNewTag = createNew;

            TempTag = new Tag(EditedTag);
        }
        #region Properties

        private Tag EditedTag;
        public bool CreateNewTag { get; init; }

        //TempTag to work with
        private Tag tempTag;
        public Tag TempTag
        {
            get => tempTag;
            set => SetProperty(ref tempTag, value);
        }

        //visibility of Color Selection
        private bool showcolorselection = false;
        public bool ShowColorSelection
        {
            get => showcolorselection;
            set
            {
                SetProperty(ref showcolorselection, value);
                RaisePropertyChanged(nameof(ShowInfoGrid));
            }
        }

        //visibility of General Info Selection
        public bool ShowInfoGrid => !ShowColorSelection;

        #endregion

        #region Functions and Commands

        private ActionCommand _oKCommand;
        public ActionCommand OKCommand => _oKCommand ??= new(OKBtn, canOkBtn);
        public void OKBtn()
        {
            //Apply changes 
            EditedTag.Copy(TempTag);

            if (CreateNewTag)
            {
                if (TempTag.Parent is null) MainViewModel.CollectionVM.CurrentCollection.RootTags.Add(EditedTag);
                else TempTag.Parent.Children.Add(EditedTag);
                EditedTag.ID = Utils.GetAvailableID(MainViewModel.CollectionVM.CurrentCollection.AllTags);
                MainViewModel.CollectionVM.CurrentCollection.AllTags.Add(EditedTag);
            }

            MainViewModel.CollectionVM.TagsVM.BuildTagTreeView();

            //reset fields
            TempTag = new(MainViewModel.CollectionVM.CurrentCollection.AllTags);
            EditedTag = new();
            CloseAction();
        }
        public bool canOkBtn() => !String.IsNullOrWhiteSpace(TempTag.Content);

        private ActionCommand _cancelCommand;
        public ActionCommand CancelCommand => _cancelCommand ??= new(Cancel);
        public void Cancel()
        {
            if (CreateNewTag)
            {
                TempTag = new Tag(MainViewModel.CollectionVM.CurrentCollection.AllTags);
            }
            EditedTag = null;
            CloseAction();
        }

        private ActionCommand _closeColorSelectionCommand;
        public ActionCommand CloseColorSelectionCommand => _closeColorSelectionCommand ??= new(CloseColorSelection);

        private ActionCommand _colorSameAsParentCommand;
        public ActionCommand ColorSameAsParentCommand => _colorSameAsParentCommand ??= new(SetColorSameAsParent);
        private void SetColorSameAsParent()
        {
            TempTag.SerializableBackgroundColor = null;
            CloseColorSelection();
        }

        private void CloseColorSelection() => ShowColorSelection = false;
        public Action CloseAction { get; set; } = new(() => { });

        #endregion
    }
}
