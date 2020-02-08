using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class BaseViewModel : ObservableObject
    { 
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
    }
}
