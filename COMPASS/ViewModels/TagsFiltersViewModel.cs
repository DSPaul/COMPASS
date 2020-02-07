using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class TagsFiltersViewModel : BaseViewModel
    {
        public TagsFiltersViewModel(MainViewModel vm)
        {
            MVM = vm;
            EditTagCommand = new BasicCommand(EditTag);
            DeleteTagCommand = new BasicCommand(DeleteTag);
        }

        #region Properties

        //MainViewModel
        private MainViewModel mainViewModel;
        public MainViewModel MVM
        {
            get { return mainViewModel; }
            set { SetProperty(ref mainViewModel, value); }
        }

        //selected Tag in Treeview
        public Tag SelectedTag
        {
            get
            {
                foreach(Tag t in MVM.CurrentData.AllTags)
                {
                    if (t.Check) return t;
                }
                return null;
            }
            set 
            {
                foreach (Tag t in MVM.CurrentData.AllTags)
                {
                    if (t == value) t.Check = true;
                    else t.Check = false;
                }
            }
        }

        //Tag for Context Menu
        public Tag Context;

        #endregion

        #region Functions and Commands

        public BasicCommand EditTagCommand { get; private set;}
        public void EditTag()
        {
            if (Context != null)
            {
                MVM.CurrentEditViewModel = new TagEditViewModel(MVM, Context);
                TagPropWindow tpw = new TagPropWindow((TagEditViewModel)MVM.CurrentEditViewModel);
                tpw.Show();
            }
        }

        public BasicCommand DeleteTagCommand { get; private set; }
        public void DeleteTag()
        {
            if (Context == null) return;
            MVM.CurrentData.DeleteTag(Context);
            //Go over all files and refresh tags list
            foreach (var f in MVM.CurrentData.AllFiles)
            {
                int i = 0;
                //iterate over all the tags in the file
                while (i < f.Tags.Count)
                {
                    Tag currenttag = f.Tags[i];
                    //try to find the tag in alltags, if found, increase i to go to next tag
                    try
                    {
                        MVM.CurrentData.AllTags.First(tag => tag.ID == currenttag.ID);
                        i++;
                    }
                    //if the tag in not found in alltags, delete it
                    catch (System.InvalidOperationException)
                    {
                        f.Tags.Remove(currenttag);
                    }
                }
            }
            MVM.Reset();
            SelectedTag = null;
        }

        #endregion
    }
}
