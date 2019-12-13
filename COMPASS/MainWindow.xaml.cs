using System;
using System.IO;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;
using ImageMagick;

namespace COMPASS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            LoadTags();
            LoadFiles();
            MagickNET.SetGhostscriptDirectory(@"C:\Users\pauld\Documents\COMPASS\COMPASS\Libraries");
            
            //set Itemsources for databinding
            CurrentTagList.ItemsSource = Data.ActiveTags;
            FileListView.ItemsSource = Data.ActiveFiles;
            FileMixView.ItemsSource = Data.ActiveFiles;
            FileTileView.ItemsSource = Data.ActiveFiles;
            TagTree.DataContext = Data.RootTags;
            ParentSelectionTree.DataContext = Data.RootTags;
            ParentSelectionPanel.DataContext = ParentSelectionTree.SelectedItem as Tag;
            //

            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        }

        // is true if we hold left mouse button on windows tilebar
        private bool DragWindow = false;

        //Loads the RootTags from a file and constructs the Alltags list from it
        public void LoadTags()
        {
            if (File.Exists(@"C:\Users\pauld\Documents\Tags.xml"))
            {
                //loading root tags
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<Tag>));
                using (var Reader = new StreamReader(@"C:\Users\pauld\Documents\Tags.xml"))
                {
                    Data.RootTags = serializer.Deserialize(Reader) as ObservableCollection<Tag>;
                }

                //Creating All Tags
                List<Tag> Currentlist = Data.RootTags.ToList();
                for(int i = 0; i < Currentlist.Count();i++)
                {
                    Tag t = Currentlist[i];
                    Data.AllTags.Add(t);
                    if(t.Items.Count > 0)
                    {
                        foreach (Tag t2 in t.Items) Currentlist.Add(t2);
                    }   
                }
            }
        }

        //Loads AllFiles list from Files
        public void LoadFiles()
        {
            if (File.Exists(@"C:\Users\pauld\Documents\Files.xml"))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<MyFile>));
                using (var Reader = new StreamReader(@"C:\Users\pauld\Documents\Files.xml"))
                {
                    Data.AllFiles = serializer.Deserialize(Reader) as ObservableCollection<MyFile>;
                }

                //build metadatalists
                foreach(MyFile f in Data.AllFiles)
                {
                    if (f.Author != "" && !Data.AuthorList.Contains(f.Author)) Data.AuthorList.Add(f.Author);
                    if (f.Publisher != "" && !Data.PublisherList.Contains(f.Publisher)) Data.PublisherList.Add(f.Publisher);
                    if (f.Source != "" && !Data.SourceList.Contains(f.Source)) Data.SourceList.Add(f.Source);
                }
            }
            Reset();
        }

        //resets Filters and searchfield
        public void Reset()
        {
            ClearTreeViewSelection(TagTree);

            Data.TagFilteredFiles.Clear();
            Data.SearchFilteredFiles.Clear();
            foreach (MyFile f in Data.AllFiles)
            {
                Data.SearchFilteredFiles.Add(f);
                Data.TagFilteredFiles.Add(f);
            }
            Update_ActiveFiles();
            Searchbox.Text = "Search";
            Data.ActiveTags.Clear();

            RefreshTreeViews();
        }

        public void RefreshTreeViews()
        {
            //redraws treeviews
            TagTree.DataContext = null;
            ParentSelectionTree.DataContext = null;
            TagTree.DataContext = Data.RootTags;
            ParentSelectionTree.DataContext = Data.RootTags;
        }

        //Activates filter when a tag is clicked in the treeview
        private void Tag_Selected(object sender, RoutedEventArgs e)
        {
            //var track = ((TreeView)sender).SelectedItem as System.Windows.Controls.Primitives.Track; //Casting back to the binded Track
            dynamic selectedtag = TagTree.SelectedItem;
            if (selectedtag == null) return;
            if (Data.ActiveTags.All(p => p.ID != selectedtag.ID)) 
            {
                Data.ActiveTags.Add(selectedtag);
            }   
            foreach(MyFile f in Data.AllFiles)
            {
                if (!f.Tags.Contains(selectedtag) && Data.TagFilteredFiles.Contains(f)) Data.TagFilteredFiles.Remove(f);
            }
            Update_ActiveFiles();
            ClearTreeViewSelection(TagTree);
        }

        //Convert PDFs to image previews
        public void ConvertPDF(MyFile pdf)
        {
            MagickReadSettings settings = new MagickReadSettings()
            {
                Density = new Density(100, 100),
                FrameIndex = 0, // First page
                FrameCount = 1, // Number of pages
            };
            using (MagickImage image = new MagickImage())
            {
                // Add all the pages of the pdf file to the collection
                image.Read(pdf.Path, settings);
                image.Format = MagickFormat.Png;
                image.Trim();
                image.Alpha(AlphaOption.Remove);

                image.Write(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\CoverArt\" + pdf.ID.ToString() + ".png");
            }
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

        //Opens tag creation popup
        private void Addtag_Click(object sender, RoutedEventArgs e)
        {
            if (TagCreation.Visibility == Visibility.Collapsed) TagCreation.Visibility = Visibility.Visible;
            else TagCreation.Visibility = Visibility.Collapsed;
        }

        //Deselects when you click away
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainGrid.Focus();
        }

        //resets all filters
        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        //removes tag from filter list when clicked
        private void ActiveTag_Click(object sender, RoutedEventArgs e)
        {
            Data.ActiveTags.Remove((Tag)CurrentTagList.SelectedItem);
            foreach (Tag t in Data.ActiveTags) if (t == (Tag)CurrentTagList.SelectedItem) Data.ActiveTags.Remove(t);
            Data.TagFilteredFiles.Clear();
            foreach(MyFile f in Data.AllFiles)
            {
                if(Data.ActiveTags.All(i => f.Tags.Contains(i)))
                    Data.TagFilteredFiles.Add(f);
            }
            Update_ActiveFiles();
        }

        //import files
        private void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AddExtension = false,
                Multiselect = true   
            };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach(string path in openFileDialog.FileNames)
                {
                    if(Data.AllFiles.All(p => p.Path != path))
                    {
                    MyFile pdf = new MyFile { Path = path, Title = System.IO.Path.GetFileNameWithoutExtension(path)};
                        Data.AllFiles.Add(pdf);
                        ConvertPDF(pdf);
                    }
                }
                Reset();
            }         
        }

        //Open file on doubleclick
        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileListView.Visibility == Visibility.Visible)
            {
                dynamic selectedItem = FileListView.SelectedItem;
                Process.Start(selectedItem.Path);
            }

            if (FileMixView.Visibility == Visibility.Visible)
            {
                dynamic selectedItem = FileMixView.SelectedItem;
                Process.Start(selectedItem.Path);
            }

            if (FileTileView.Visibility == Visibility.Visible)
            {
                dynamic selectedItem = FileTileView.SelectedItem;
                Process.Start(selectedItem.Path);
            }
        }

        private void TagNameTextBlock_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return && TagCreation.Visibility == Visibility.Visible)
            {
                CreateTagBtn_Click(sender,e);
            }
        }

        private void Searchbtn_Click(object sender, RoutedEventArgs e)
        {
            Data.SearchFilteredFiles.Clear();
            foreach (MyFile f in Data.AllFiles) Data.SearchFilteredFiles.Add(f);

            if (Searchbox.Text == "" || Searchbox.Text == "Search")
            {
                Searchbox.Text = "Search";
            }
            else
            {
                foreach(MyFile f in Data.AllFiles)
                {
                    if (f.Title.IndexOf(Searchbox.Text, StringComparison.OrdinalIgnoreCase) < 0) Data.SearchFilteredFiles.Remove(f);
                }
            }
            Update_ActiveFiles();
        }

        public static void Update_ActiveFiles()
        {
            Data.ActiveFiles.Clear();
            foreach (var p in Data.TagFilteredFiles.Intersect(Data.SearchFilteredFiles))
                Data.ActiveFiles.Add(p);
        }

        private void Searchbox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return) Searchbtn_Click(sender, e);
        }

        private void Searchbox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Searchbox.Text = "";
        }

        //EDIT File
        private void Edit_File_Btn(object sender, RoutedEventArgs e)
        {
            Data.EditedFile = FileListView.SelectedItem as MyFile;
            FilePropWindow fpw = new FilePropWindow();
            fpw.Show();
        }

        public static void SaveTagsToFile()
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<Tag>));
            using (var writer = new StreamWriter(@"C:\Users\pauld\Documents\Tags.xml"))
            {
                serializer.Serialize(writer, Data.RootTags);
            }
        }

        public static void SaveFilesToFile()
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<MyFile>));
            using (var writer = new StreamWriter(@"C:\Users\pauld\Documents\Files.xml"))
            {
                serializer.Serialize(writer, Data.AllFiles);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveFilesToFile();
            SaveTagsToFile();
        }

        private void CreateTagBtn_Click(object sender, RoutedEventArgs e)
        {
            Tag newtag = new Tag() { Content = TagNameTextBlock.Text, BackgroundColor = (Color)ColorSelector.SelectedColor};
            if (ParentSelectionTree.SelectedItem != null)
            {
                Tag Parent = ParentSelectionTree.SelectedItem as Tag;
                Parent.Expanded = true;
                Parent.Items.Add(newtag);
                newtag.ParentID = Parent.ID;
            }
            else
            {
                Data.RootTags.Add(newtag);
            }

            Data.AllTags.Add(newtag);
            TagNameTextBlock.Text = "";
            TagCreation.Visibility = Visibility.Collapsed;
        }

        private void ShowParentSelection_Click(object sender, RoutedEventArgs e)
        {
            if (ParentSelection.Visibility == Visibility.Collapsed && sender.GetType().Name == "Button")
            {
                ParentSelection.Visibility = Visibility.Visible;
                TagCreationMain.Visibility = Visibility.Collapsed;
            }
            else
            {
                ParentSelectionPanel.DataContext = null;
                ParentSelectionPanel.DataContext = ParentSelectionTree.SelectedItem as Tag;
                ParentSelection.Visibility = Visibility.Collapsed;
                TagCreationMain.Visibility = Visibility.Visible;
            }  
        }

        private void ShowColorSelection_Click(object sender, RoutedEventArgs e)
        {
            if (ColorSelection.Visibility == Visibility.Collapsed)
            {
                ColorSelection.Visibility = Visibility.Visible;
                TagCreationMain.Visibility = Visibility.Collapsed;
            }
            else
            {                
                ColorSelection.Visibility = Visibility.Collapsed;
                TagCreationMain.Visibility = Visibility.Visible;
            }
        }

        #region View Selection Buttons
                //*****************View Selection **************
        public void ShowListView(object sender, RoutedEventArgs e)
        {
            FileListView.Visibility = Visibility.Visible;
            FileMixView.Visibility = Visibility.Collapsed;
            FileTileView.Visibility = Visibility.Collapsed;
        }
        public void ShowMixView(object sender, RoutedEventArgs e)
        {
            FileListView.Visibility = Visibility.Collapsed;
            FileMixView.Visibility = Visibility.Visible;
            FileTileView.Visibility = Visibility.Collapsed;
        }
        public void ShowTileView(object sender, RoutedEventArgs e)
        {
            FileListView.Visibility = Visibility.Collapsed;
            FileMixView.Visibility = Visibility.Collapsed;
            FileTileView.Visibility = Visibility.Visible;
        }
        #endregion

        #region Windows Tile Bar Buttons
                private void MinimizeWindow(object sender, RoutedEventArgs e)
                {
                    App.Current.MainWindow.WindowState = WindowState.Minimized;
                }
                private void MaximizeClick(object sender, RoutedEventArgs e)
                {
                    if (App.Current.MainWindow.WindowState == WindowState.Maximized)
                    {
                        App.Current.MainWindow.WindowState = WindowState.Normal;
                        Maximizeimage.Visibility = Visibility.Visible;
                        NotMaximizeimage.Visibility = Visibility.Collapsed;

                    }
                    else
                    {
                        App.Current.MainWindow.WindowState = WindowState.Maximized;
                        Maximizeimage.Visibility = Visibility.Collapsed;
                        NotMaximizeimage.Visibility = Visibility.Visible;
                    }
                }
                private void WindowsBar_MouseDown(object sender, MouseButtonEventArgs e)
                {
                    if (e.ClickCount == 2)
                    {
                        MaximizeClick(sender, e);
                        DragWindow = false;
                    }

                    else
                    {
                        App.Current.MainWindow.DragMove();
                        if (App.Current.MainWindow.WindowState == WindowState.Maximized)
                        {
                            DragWindow = WindowState == WindowState.Maximized;
                        }
                    }


                }
                private void OnMouseMove(object sender, MouseEventArgs e)
                {
                    if (DragWindow)
                    {
                        DragWindow = false;

                        var point = PointToScreen(e.MouseDevice.GetPosition(this));

                        Left = point.X - (RestoreBounds.Width * 0.5);
                        Top = point.Y;

                        WindowState = WindowState.Normal;

                        Maximizeimage.Visibility = Visibility.Visible;
                        NotMaximizeimage.Visibility = Visibility.Collapsed;

                        try
                        {
                            DragMove();
                        }

                        catch (InvalidOperationException)
                        {
                            MaximizeClick(sender, e);
                        }
                   
                    }
                }
                private void CloseButton_Click(object sender, RoutedEventArgs e)
                {
                    this.Close();
                }
        #endregion

        #region Context Menu Tags
        private void TagTree_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }
        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private void EditTag(object sender, RoutedEventArgs e)
        {
            Data.EditedTag = TagTree.SelectedItem as Tag;
            ClearTreeViewSelection(TagTree);
            if (Data.EditedTag != null)
            {
                TagPropWindow tpw = new TagPropWindow();
                tpw.Closed += EditTagClosedHandler;
                tpw.Show();
            }
        }

        public void EditTagClosedHandler(object sender, EventArgs e)
        {
            RefreshTreeViews();
        }

        //Delete Tag, event funtion and recursive help function
        private void DeleteTag(object sender, RoutedEventArgs e)
        {
            var todel = TagTree.SelectedItem as Tag;
            DeleteTag(todel);
            //Go over all files and refresh tags list
            foreach (var f in Data.AllFiles)
            {
                int i = 0;
                //iterate over all the tags in the file
                while (i<f.Tags.Count)
                {
                    Tag currenttag = f.Tags[i];
                    //try to find the tag in alltags, if found, increase i to go to next tag
                    try
                    {
                        Data.AllTags.First(tag => tag.ID == currenttag.ID);
                        i++;
                    }
                    //if the tag in not found in alltags, delete it
                    catch (System.InvalidOperationException)
                    {
                        f.Tags.Remove(currenttag);
                    }                  
                }
            }
            Reset();
            ClearTreeViewSelection(TagTree);
        }
        private void DeleteTag(Tag todel)
        {
            //Recursive loop to delete all childeren
            if (todel.Items.Count > 0)
            {
                DeleteTag(todel.Items[0]);
                DeleteTag(todel);
            }
            Data.AllTags.Remove(todel);
            //remove from parent items list
            if (todel.ParentID == -1) Data.RootTags.Remove(todel);
            else todel.GetParent().Items.Remove(todel);
        }
        #endregion

        private void ClearParent_Click(object sender, RoutedEventArgs e)
        {
            ClearTreeViewSelection(ParentSelectionTree);
        }

        //makes scrolwheel work in parent selection tree
        private void ParentSelectionScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }

}


