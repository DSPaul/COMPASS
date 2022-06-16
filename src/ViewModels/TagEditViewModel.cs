using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class TagEditViewModel : BaseEditViewModel
    {
        public TagEditViewModel(Tag ToEdit) : base()
        {
            EditedTag = ToEdit;
            if (ToEdit == null) CreateNewTag = true;
            TempTag = new Tag(MVM.CurrentCollection.AllTags);
            if (!CreateNewTag) TempTag.Copy(EditedTag);

            ShowColorSelection = false;

            //Commands
            CloseColorSelectionCommand = new ActionCommand(CloseColorSelection);
        }

        #region Properties

        private Tag EditedTag;
        private readonly bool CreateNewTag;

        //TempTag to work with
        private Tag tempTag;
        public Tag TempTag
        {
            get { return tempTag; }
            set { SetProperty(ref tempTag, value); }
        }

        //visibility of Color Selection
        private bool showcolorselection = false;
        public bool ShowColorSelection
        {
            get { return showcolorselection; }
            set 
            { 
                SetProperty(ref showcolorselection, value);
                RaisePropertyChanged(nameof(ShowInfoGrid));
            }
        }

        //visibility of General Info Selection
        public bool ShowInfoGrid
        {
            get { return !ShowColorSelection; }
            set { }
        }

        #endregion

        #region Functions and Commands

        public override void OKBtn()
        {
            if(CreateNewTag)
            {
                EditedTag = new Tag(CurrentCollection.AllTags);
                if (TempTag.ParentID == -1) CurrentCollection.RootTags.Add(EditedTag);
            }

            //Apply changes 
            EditedTag.Copy(TempTag);
            MVM.TFViewModel.TagsTabVM.RefreshTreeView();

            if (!CreateNewTag) CloseAction();
            else
            {
                CurrentCollection.AllTags.Add(EditedTag);
                //reset fields
                TempTag = new Tag(CurrentCollection.AllTags);
                EditedTag = null;
            }
        }

        public override void Cancel()
        {
            if (!CreateNewTag) CloseAction();
            else
            {
                TempTag = new Tag(CurrentCollection.AllTags);
            }
            EditedTag = null;
        }

        public ActionCommand CloseColorSelectionCommand { get; private set; }
        private void CloseColorSelection()
        {
            ShowColorSelection = false;
        }

        #endregion
    }
}
