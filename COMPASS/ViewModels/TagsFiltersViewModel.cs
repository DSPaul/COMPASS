using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class TagsFiltersViewModel : BaseViewModel
    {
        public TagsFiltersViewModel(MainViewModel vm)
        {
            MVM = vm;
            TreeViewSource = CreateTreeViewSourceFromCollection(MVM.CurrentData.RootTags);
            AllTreeViewNodes = CreateAllTreeViewNodes(TreeViewSource);
            EditTagCommand = new BasicCommand(EditTag);
            DeleteTagCommand = new BasicCommand(DeleteTag);
            ClearFiltersCommand = new BasicCommand(ClearFilters);
        }

        #region Properties

        //MainViewModel
        private MainViewModel mainViewModel;
        public MainViewModel MVM
        {
            get { return mainViewModel; }
            set { SetProperty(ref mainViewModel, value); }
        }

        //TreeViewSource
        private ObservableCollection<TreeViewNode> treeviewsource;
        public ObservableCollection<TreeViewNode> TreeViewSource
        {
            get { return treeviewsource; }
            set { SetProperty(ref treeviewsource, value); }
        }

        //AllTreeViewNodes For iterating
        private ObservableCollection<TreeViewNode> alltreeViewNodes;
        public ObservableCollection<TreeViewNode> AllTreeViewNodes
        {
            get { return alltreeViewNodes; }
            set { SetProperty(ref alltreeViewNodes, value); }
        }

        //selected Tag in Treeview
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
                foreach (TreeViewNode t in AllTreeViewNodes)
                {
                    if (t.Tag == value) t.Selected = true;
                    else t.Selected = false;
                }
            }
        }

        //Selected Autor in FilterTab
        private string selectedAuthor;
        public string SelectedAuthor
        {
            get { return selectedAuthor; }
            set
            {
                SetProperty(ref selectedAuthor, value);
                FilterTag AuthorTag = new FilterTag(MVM.FilterHandler.ActiveFilters,MetaData.Author) { Content = "Author: " + value, BackgroundColor = Colors.Orange };
                MVM.FilterHandler.ActiveFilters.Add(AuthorTag);
            }
        }

        //Selected Autor in FilterTab
        private string selectedPublisher;
        public string SelectedPublisher
        {
            get { return selectedPublisher; }
            set
            {
                SetProperty(ref selectedPublisher, value);
                FilterTag PublTag = new FilterTag(MVM.FilterHandler.ActiveFilters,MetaData.Publisher) { Content = "Publisher: " + value, BackgroundColor = Colors.MediumPurple };
                MVM.FilterHandler.ActiveFilters.Add(PublTag);
            }
        }

        //Tag for Context Menu
        public Tag Context;

        #endregion

        #region Functions and Commands
        //-------------------For Tags Tab ---------------------//
        public BasicCommand EditTagCommand { get; private set; }
        public void EditTag()
        {
            if (Context != null)
            {
                MVM.CurrentEditViewModel = new TagEditViewModel(MVM, Context);
                TagPropWindow tpw = new TagPropWindow((TagEditViewModel)MVM.CurrentEditViewModel);
                tpw.ShowDialog();
                tpw.Topmost = true;
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
            TreeViewSource = CreateTreeViewSourceFromCollection(MVM.CurrentData.RootTags);
            AllTreeViewNodes = CreateAllTreeViewNodes(TreeViewSource);
            MVM.Reset();

            //SelectedTag = null;
        }

        public void RefreshTreeView()
        {
            TreeViewSource = CreateTreeViewSourceFromCollection(MVM.CurrentData.RootTags);
            AllTreeViewNodes = CreateAllTreeViewNodes(TreeViewSource);
        }
        //-----------------------------------------------------//

        //----------------For Filters Tab---------------------//
        public BasicCommand ClearFiltersCommand { get; private set; }
        public void ClearFilters()
        {
            MVM.FilterHandler.ActiveFilters.Clear();
            SelectedAuthor = null;
            SelectedPublisher = null;
        }
        //-----------------------------------------------------//
        #endregion
    }
}
