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
        public TagEditViewModel(MainViewModel vm, Tag ToEdit) : base(vm)
        {
            EditedTag = ToEdit;
            TempTag = new Tag(vm.CurrentCollection.AllTags);
            if (!CreateNewTag) TempTag.Copy(EditedTag);
            else ClearParent();

            ShowColorSelection = false;
            ShowParentSelection = false;

            //Commands
            ClearParentCommand = new BasicCommand(ClearParent);
            CancelParentSelectionCommand = new BasicCommand(CancelParentSelection);
            CloseColorSelectionCommand = new BasicCommand(CloseColorSelection);
        }

        #region Properties

        private Tag EditedTag;
        private bool CreateNewTag
        {
            get { return EditedTag == null; }
        }

        //TempTag to work with
        private Tag tempTag;
        public Tag TempTag
        {
            get { return tempTag; }
            set { SetProperty(ref tempTag, value); }
        }

        //Parent of tempTag for binding
        public Tag ParentTempTag
        {
            get { return TempTag.GetParent(); }
            set 
            { 
                TempTag.ParentID = value.ID;
                RaisePropertyChanged();
            }
        }

        //visibility of parenttree
        private bool showparentselection = false;
        public bool ShowParentSelection
        {
            get { return showparentselection; }
            set 
            {                
                SetProperty(ref showparentselection, value);
                RaisePropertyChanged("ShowInfoGrid");
            }
        }

        //visibility of Color Selection
        private bool showcolorselection = false;
        public bool ShowColorSelection
        {
            get { return showcolorselection; }
            set 
            { 
                SetProperty(ref showcolorselection, value);
                RaisePropertyChanged("ShowInfoGrid");
            }
        }

        //visibility of General Info Selection
        public bool ShowInfoGrid
        {
            get { return !(ShowParentSelection || ShowColorSelection); }
            set { }
        }

        //Selected Parent from Treeview
        public Tag SelectedTag
        {
            get
            {
                foreach (TreeViewNode t in AllTreeViewNodes)
                {
                    if (t.Selected) return t.Tag;
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    TempTag.ParentID = -1;
                    foreach (TreeViewNode t in AllTreeViewNodes)
                    {
                        t.Selected = false;
                    }
                }

                else
                {
                    foreach (TreeViewNode t in AllTreeViewNodes)
                    {
                        if (t.Tag == value)
                        {
                            t.Selected = true;
                            //Set Parent tag when different tag is selected
                            ParentTempTag = t.Tag;
                            ShowParentSelection = false;
                        }
                    }
                }
                
            }
        }

        #endregion

        #region Functions and Commands

        //Refreshes all the files to apply changes in Tag
        public BasicCommand ClearParentCommand { get; private set; }
        private void ClearParent()
        {
            SelectedTag = null;
            RaisePropertyChanged("ParentTempTag");
        }

        public override void OKBtn()
        {
            bool CreatingTag = false;
            if(CreateNewTag)
            {
                EditedTag = new Tag(CurrentCollection.AllTags);
                CreatingTag = true;
                if (TempTag.ParentID == -1) CurrentCollection.RootTags.Add(EditedTag);
            }
            //set Parent if changed
            if (EditedTag.ParentID != tempTag.ParentID)
            {
                if (EditedTag.ParentID == -1) CurrentCollection.RootTags.Remove(EditedTag);
                else EditedTag.GetParent().Items.Remove(tempTag);

                if (TempTag.ParentID == -1) CurrentCollection.RootTags.Add(EditedTag);
                else TempTag.GetParent().Items.Add(EditedTag);
            }
            //Apply changes 
            EditedTag.Copy(TempTag);
            MVM.TFViewModel.RefreshTreeView();
            if (!CreatingTag) CloseAction();
            else
            {
                CurrentCollection.AllTags.Add(EditedTag);
                TempTag = new Tag(CurrentCollection.AllTags);
                EditedTag = null;
                RaisePropertyChanged("ParentTempTag");
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

        public BasicCommand CancelParentSelectionCommand { get; private set;}
        private void CancelParentSelection()
        {
            ShowParentSelection = false;
        }

        public BasicCommand CloseColorSelectionCommand { get; private set; }
        private void CloseColorSelection()
        {
            ShowColorSelection = false;
        }

        #endregion
    }
}
