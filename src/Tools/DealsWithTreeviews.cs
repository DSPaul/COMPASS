using COMPASS.Models;
using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.Tools
{
    public abstract class DealsWithTreeviews:ObservableObject,IDropTarget
    {
        public DealsWithTreeviews(CodexCollection CC)
        {
            TreeViewSource = CreateTreeViewSource(CC.RootTags);
            AllTreeViewNodes = CreateAllTreeViewNodes(TreeViewSource);

            cc = CC;
        }

        private CodexCollection cc;

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

        /***All Edit View Models deal with Treeviews so putting treeview related functions here***/
        public ObservableCollection<TreeViewNode> CreateTreeViewSource(List<Tag> RootTags)
        {
            ObservableCollection<TreeViewNode> newRootNodes = new ObservableCollection<TreeViewNode>();
            foreach (Tag t in RootTags)
            {
                newRootNodes.Add(ConvertTagToTreeViewNode(t));
            }

            return newRootNodes;
        }

        public List<Tag> ExtractTagsFromTreeViewSource(ObservableCollection<TreeViewNode> treeViewSource)
        {
            var newRootTags = new List<Tag>();
            foreach (TreeViewNode n in treeViewSource)
            {
                newRootTags.Add(ConvertTreeViewNodeToTag(n));
            }
            foreach(Tag t in newRootTags)
            {
                t.ParentID = -1;
            }
            return newRootTags;
        }
        private TreeViewNode ConvertTagToTreeViewNode(Tag t)
        {
            TreeViewNode Result = new TreeViewNode(t);
            foreach (Tag t2 in t.Items) Result.Children.Add(ConvertTagToTreeViewNode(t2));
            return Result;
        }

        private Tag ConvertTreeViewNodeToTag(TreeViewNode node)
        {
            Tag Result = node.Tag;
            //clear childeren the tag thinks it has
            Result.Items.Clear();

            //add childeren accodring to treeview
            foreach (TreeViewNode childnode in node.Children)
            {
                Result.Items.Add(ConvertTreeViewNodeToTag(childnode));
            }
            //set partentID for all the childeren
            foreach(Tag childtag in Result.Items)
            {
                childtag.ParentID = Result.ID;
            }

            return Result;
        }

        public ObservableCollection<TreeViewNode> CreateAllTreeViewNodes(ObservableCollection<TreeViewNode> RootNodes)
        {
            ObservableCollection<TreeViewNode> AllNodes = new ObservableCollection<TreeViewNode>();
            List<TreeViewNode> Currentlist = RootNodes.ToList();
            for (int i = 0; i < Currentlist.Count(); i++)
            {
                TreeViewNode t = Currentlist[i];
                AllNodes.Add(t);
                if (t.Children.Count > 0)
                {
                    foreach (TreeViewNode t2 in t.Children) Currentlist.Add(t2);
                }
            }
            return AllNodes;
        }

        public void RefreshTreeView()
        {
            TreeViewSource = CreateTreeViewSource(cc.RootTags);
            AllTreeViewNodes = CreateAllTreeViewNodes(TreeViewSource);
        }


        //Drop on Treeview Behaviour
        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.DragOver(dropInfo);
        }
        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.Drop(dropInfo);
            cc.RootTags = ExtractTagsFromTreeViewSource(TreeViewSource);
        }
        /*** End of Treeview section ***/
    }
}
