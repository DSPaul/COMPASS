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
            TempTag = new Tag();
            tempTag.Copy(EditedTag);

            //Commands
            ClearParentCommand = new BasicCommand(ClearParent);
            CancelParentSelectionCommand = new BasicCommand(CancelParentSelection);
            CloseColorSelectionCommand = new BasicCommand(CloseColorSelection);
        }

        #region Properties

        private Tag EditedTag;

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
        #endregion

        //visibility of parenttree
        private bool showparentselection = false;
        public bool ShowParentSelection
        {
            get { return showparentselection; }
            set { SetProperty(ref showparentselection, value); }
        }

        //visibility of Color Selection
        private bool showcolorselection = false;
        public bool ShowColorSelection
        {
            get { return showcolorselection; }
            set { SetProperty(ref showcolorselection, value); }
        }

        //Selected Item for Treeview
        public Tag SelectedTag
        {
            get
            {
                foreach (Tag t in MVM.CurrentData.AllTags)
                {
                    if (t.Check) return t;
                }
                return null;
            }
            set
            {
                foreach (Tag t in MVM.CurrentData.AllTags)
                {
                    if (t == value)
                    {
                        t.Check = true;
                        //Set Parent tag when different tag is selected
                        ParentTempTag = t;
                        ShowParentSelection = false;
                    }
                }
            }
        }

        #region Functions and Commands

        //Refreshes all the files to apply changes in Tag
        private void UpdateAllFiles()
        {
            foreach (var f in MVM.CurrentData.AllFiles.Where(f => f.Tags.Contains(TempTag)))
            {
                foreach (Tag t in MVM.CurrentData.AllTags)
                {
                    if (f.Tags.Contains(t))
                    {
                        t.Check = true;
                    }
                    else
                    {
                        t.Check = false;
                    }
                }
                f.Tags.Clear();
                foreach (Tag t in MVM.CurrentData.AllTags)
                {
                    if (t.Check)
                    {
                        f.Tags.Add(t);
                    }
                    t.Check = false;
                }
            }
        }

        public BasicCommand ClearParentCommand { get; private set; }
        private void ClearParent()
        {
            ParentTempTag.ID = -1;
        }

        public override void OKBtn()
        {
            //set Parent if changed
            if (EditedTag.ParentID != tempTag.ParentID)
            {
                if (EditedTag.ParentID == -1)
                {
                    MVM.CurrentData.RootTags.Remove(EditedTag);
                }
                else
                {
                    EditedTag.GetParent().Items.Remove(tempTag);
                }

                if (tempTag.ParentID == -1)
                {
                    MVM.CurrentData.RootTags.Add(EditedTag);
                }
                else
                {
                    tempTag.GetParent().Items.Add(EditedTag);
                }
            }
            //Apply changes 
            EditedTag.Copy(tempTag);
            UpdateAllFiles();
            CloseAction();
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
