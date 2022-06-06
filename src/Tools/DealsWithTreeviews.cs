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
            cc = CC;
            RefreshTreeView();
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
        private HashSet<TreeViewNode> alltreeViewNodes;
        public HashSet<TreeViewNode> AllTreeViewNodes
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
            foreach (Tag t2 in t.Children) Result.Children.Add(ConvertTagToTreeViewNode(t2));
            return Result;
        }

        private Tag ConvertTreeViewNodeToTag(TreeViewNode node)
        {
            Tag Result = node.Tag;
            //clear childeren the tag thinks it has
            Result.Children.Clear();

            //add childeren accodring to treeview
            foreach (TreeViewNode childnode in node.Children)
            {
                Result.Children.Add(ConvertTreeViewNodeToTag(childnode));
            }
            //set partentID for all the childeren
            foreach(Tag childtag in Result.Children)
            {
                childtag.ParentID = Result.ID;
            }

            return Result;
        }

        public void RefreshTreeView()
        {
            TreeViewSource = CreateTreeViewSource(cc.RootTags);
            AllTreeViewNodes = Utils.FlattenTree(TreeViewSource).ToHashSet();
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
