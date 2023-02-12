using COMPASS.Commands;
using COMPASS.Models;
using System;

namespace COMPASS.ViewModels
{
    public class TagEditViewModel : ViewModelBase, IEditViewModel
    {
        public TagEditViewModel(Tag ToEdit, bool isGroup = false) : base()
        {
            EditedTag = ToEdit;
            TempTag = new Tag(MVM.CurrentCollection.AllTags);

            if (ToEdit is null)
            {
                CreateNewTag = true;
                TempTag.IsGroup = isGroup;
            }
            else
            {
                TempTag.Copy(EditedTag);
            }

            //Commands
            CloseColorSelectionCommand = new ActionCommand(CloseColorSelection);
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
        public ActionCommand OKCommand => _oKCommand ??= new(OKBtn);
        public void OKBtn()
        {
            if (CreateNewTag)
            {
                EditedTag = new Tag(MVM.CurrentCollection.AllTags);
                if (TempTag.Parent is null) MVM.CurrentCollection.RootTags.Add(EditedTag);
            }

            //Apply changes 
            EditedTag.Copy(TempTag);
            MVM.LeftDockVM.TagsTabVM.RefreshTreeView();

            if (!CreateNewTag) CloseAction();
            else
            {
                MVM.CurrentCollection.AllTags.Add(EditedTag);
                //reset fields
                TempTag = new Tag(MVM.CurrentCollection.AllTags);
                EditedTag = null;
            }
        }

        private ActionCommand _cancelCommand;
        public ActionCommand CancelCommand => _cancelCommand ??= new(Cancel);
        public void Cancel()
        {
            if (!CreateNewTag) CloseAction();
            else
            {
                TempTag = new Tag(MVM.CurrentCollection.AllTags);
            }
            EditedTag = null;
        }

        public ActionCommand CloseColorSelectionCommand { get; private set; }
        public Action CloseAction { get; set; }

        private void CloseColorSelection() => ShowColorSelection = false;

        #endregion
    }
}
