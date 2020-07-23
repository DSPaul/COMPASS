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
                FilterTag AuthorTag = new FilterTag(MVM.FilterHandler.ActiveFilters,FilterType.Author,value) { Content = "Author: " + value, BackgroundColor = Colors.Orange };
                MVM.FilterHandler.ActiveFilters.Add(AuthorTag);
            }
        }

        //Selected Publisher in FilterTab
        private string selectedPublisher;
        public string SelectedPublisher
        {
            get { return selectedPublisher; }
            set
            {
                SetProperty(ref selectedPublisher, value);
                FilterTag PublTag = new FilterTag(MVM.FilterHandler.ActiveFilters,FilterType.Publisher,value) { Content = "Publisher: " + value, BackgroundColor = Colors.MediumPurple };
                MVM.FilterHandler.ActiveFilters.Add(PublTag);
            }
        }

        //Selected Start and Stop Release Dates
        private DateTime? startReleaseDate;
        private DateTime? stopReleaseDate;

        public DateTime? StartReleaseDate
        {
            get { return startReleaseDate; }
            set
            {
                SetProperty(ref startReleaseDate, value);
                if(value != null)
                {
                    FilterTag startDateTag = new FilterTag(MVM.FilterHandler.ActiveFilters, FilterType.StartReleaseDate, value) { Content = "After: " + value.Value.Date.ToShortDateString(), BackgroundColor = Colors.DeepSkyBlue };
                    //Remove existing start date, replacing it
                    MVM.FilterHandler.ActiveFilters.Remove(MVM.FilterHandler.ActiveFilters.Where(filter => (FilterType)filter.GetGroup() == FilterType.StartReleaseDate).FirstOrDefault());
                    MVM.FilterHandler.ActiveFilters.Add(startDateTag);
                }
            }
        }

        public DateTime? StopReleaseDate
        {
            get { return stopReleaseDate; }
            set
            {
                SetProperty(ref stopReleaseDate, value);
                if(value!= null)
                {
                    FilterTag stopDateTag = new FilterTag(MVM.FilterHandler.ActiveFilters, FilterType.StopReleaseDate,value) { Content = "Before: " + value.Value.Date.ToShortDateString(), BackgroundColor = Colors.DeepSkyBlue };
                    //Remove existing end date, replacing it
                    MVM.FilterHandler.ActiveFilters.Remove(MVM.FilterHandler.ActiveFilters.Where(filter => (FilterType)filter.GetGroup() == FilterType.StopReleaseDate).FirstOrDefault());
                    MVM.FilterHandler.ActiveFilters.Add(stopDateTag);
                }
            }
        }

        //Selected minimum rating
        private int minRating;
        public int MinRating
        {
            get { return minRating; }
            set
            {
                SetProperty(ref minRating, value);
                if(value>0 && value< 6)
                {
                    FilterTag minRatTag = new FilterTag(MVM.FilterHandler.ActiveFilters, FilterType.MinimumRating, value) { Content = "At least " + value + " stars", BackgroundColor = Colors.Goldenrod };
                    //Remove existing minimum rating, replacing it
                    MVM.FilterHandler.ActiveFilters.Remove(MVM.FilterHandler.ActiveFilters.Where(filter => (FilterType)filter.GetGroup() == FilterType.MinimumRating).FirstOrDefault());
                    MVM.FilterHandler.ActiveFilters.Add(minRatTag);
                }
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
            SelectedAuthor = null;
            SelectedPublisher = null;
            StartReleaseDate = null;
            StopReleaseDate = null;
            minRating = 0;
            MVM.FilterHandler.ActiveFilters.Clear();
        }
        //-----------------------------------------------------//
        #endregion
    }
}
