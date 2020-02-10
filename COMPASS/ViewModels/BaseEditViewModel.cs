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
    public class BaseEditViewModel : BaseViewModel
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

        #endregion

        #region Functions and Commamnds

        public Action CloseAction { get; set; }

        public BasicCommand CancelCommand { get; private set; }
        public virtual void Cancel(){}

        public BasicCommand OKCommand { get; private set; }
        public virtual void OKBtn() { }

        #endregion
    }
}
