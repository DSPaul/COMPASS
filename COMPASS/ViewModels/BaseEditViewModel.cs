using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class BaseEditViewModel : ObservableObject
    {
        public BaseEditViewModel(MainViewModel vm)
        {
            MVM = vm;
            TreeViewSource = CreateTreeViewSourceFromCollection(MVM.CurrentData.RootTags);
            AllTreeViewNodes = CreateAllTreeViewNodes(TreeViewSource);
            CancelCommand = new BasicCommand(Cancel);
            OKCommand = new BasicCommand(OKBtn);
        }

        #region Properties

        //MainViewModel
        private MainViewModel mainViewModel;
        public MainViewModel MVM
        {
            get { return mainViewModel; }
            set { SetProperty(ref mainViewModel, value); }
        }

        //TreeViewSource with hierarchy
        private ObservableCollection<TreeViewNode> treeviewsource;
        public ObservableCollection<TreeViewNode> TreeViewSource
        {
            get { return treeviewsource; }
            set { SetProperty(ref treeviewsource, value); }
        }

        //AllTreeViewNodes without hierarchy for iterating purposes
        private ObservableCollection<TreeViewNode> alltreeViewNodes;
        public ObservableCollection<TreeViewNode> AllTreeViewNodes
        {
            get { return alltreeViewNodes; }
            set { SetProperty(ref alltreeViewNodes, value); }
        }

        #endregion

        #region Functions and Commamnds

        /***All Edit View Models deal with Treeviews so putting treeview related functions here***/
        public ObservableCollection<TreeViewNode> CreateTreeViewSourceFromCollection(ObservableCollection<Tag> RootTags)
        {
            ObservableCollection<TreeViewNode> newRootTags = new ObservableCollection<TreeViewNode>();
            foreach (Tag t in RootTags)
            {
                newRootTags.Add(ConvertTagToTreeViewNode(t));
            }
            return newRootTags;
        }

        private TreeViewNode ConvertTagToTreeViewNode(Tag t)
        {
            TreeViewNode Result = new TreeViewNode(t);
            foreach (Tag t2 in t.Items) Result.Children.Add(ConvertTagToTreeViewNode(t2));
            return Result;
        }

        public ObservableCollection<TreeViewNode> CreateAllTreeViewNodes(ObservableCollection<TreeViewNode> RootTags)
        {
            ObservableCollection<TreeViewNode> AllTags = new ObservableCollection<TreeViewNode>();
            List<TreeViewNode> Currentlist = RootTags.ToList();
            for (int i = 0; i < Currentlist.Count(); i++)
            {
                TreeViewNode t = Currentlist[i];
                AllTags.Add(t);
                if (t.Children.Count > 0)
                {
                    foreach (TreeViewNode t2 in t.Children) Currentlist.Add(t2);
                }
            }
            return AllTags;
        }

        public void RefreshTreeView()
        {
            TreeViewSource = CreateTreeViewSourceFromCollection(MVM.CurrentData.RootTags);
            AllTreeViewNodes = CreateAllTreeViewNodes(TreeViewSource);
        }
        /*** End of Treeview section ***/

        public Action CloseAction { get; set; }

        public BasicCommand CancelCommand { get; private set; }
        public virtual void Cancel(){}

        public BasicCommand OKCommand { get; private set; }
        public virtual void OKBtn() { }

        #endregion
    }
}
