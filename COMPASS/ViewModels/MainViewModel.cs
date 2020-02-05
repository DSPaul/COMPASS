using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public MainViewModel(string FolderName)
        {
            currentData = new Data(FolderName);
            filterHandler = new FilterHandler(currentData);

            //Commands
            ChangeFileViewCommand = new SimpleCommand(ChangeFileView);
            ResetCommand = new SimpleCommand(Reset);
        }

        //Data 
        private Data currentData;
        public Data CurrentData
        {
            get { return currentData; }
            private set { SetProperty(ref currentData, value); }
        }

        //Filter Handler
        private FilterHandler filterHandler;
        public FilterHandler FilterHandler
        {
            get { return filterHandler; }
            private set { SetProperty(ref filterHandler, value); }
        }

        //File View Model
        private FileBaseViewModel currentFileViewModel;
        public FileBaseViewModel CurrentFileViewModel
        {
            get { return currentFileViewModel; }
            set { SetProperty(ref currentFileViewModel, value); }
        }        

        //Change Fileview
        public SimpleCommand ChangeFileViewCommand { get; private set; }
        public void ChangeFileView(Object v)
        {
            v = (FileView)v;
            switch (v)
            {
                case FileView.ListView:
                    CurrentFileViewModel = new FileListViewModel();
                    break;
                case FileView.MixView:
                    CurrentFileViewModel = new FileMixViewModel();
                    break;
                case FileView.TileView:
                    CurrentFileViewModel = new FileTileViewModel();
                    break;
            }
            CurrentFileViewModel.ActiveFiles = FilterHandler.ActiveFiles;
        }

        //Reset
        public SimpleCommand ResetCommand { get; private set; }
        public void Reset(object a = null)
        {
            //ClearTreeViewSelection(TagTree);
            FilterHandler.ClearFilters();
        }

        #region Clears Selection From TreeView
        public static void ClearTreeViewSelection(TreeView tv)
        {
            if (tv != null)
                ClearTreeViewItemsControlSelection(tv.Items, tv.ItemContainerGenerator);
        }
        private static void ClearTreeViewItemsControlSelection(ItemCollection ic, ItemContainerGenerator icg)
        {
            if ((ic != null) && (icg != null))
                for (int i = 0; i < ic.Count; i++)
                {
                    if (icg.ContainerFromIndex(i) is TreeViewItem tvi)
                    {
                        ClearTreeViewItemsControlSelection(tvi.Items, tvi.ItemContainerGenerator);
                        tvi.IsSelected = false;
                    }
                }
        }
        #endregion
    }
}
