using System;
using Microsoft.Win32;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using ImageMagick;
using COMPASS.ViewModels;

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

            MagickNET.SetGhostscriptDirectory(@"C:\Users\pauld\Documents\COMPASS\COMPASS\Libraries");

            //set Itemsources for databinding
            MainViewModel = new MainViewModel("PathFinder");
            DataContext = MainViewModel;

            ParentSelectionTree.DataContext = MainViewModel.CurrentData.RootTags;
            ParentSelectionPanel.DataContext = ParentSelectionTree.SelectedItem as Tag;

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        }

        public MainViewModel MainViewModel; 

        // is true if we hold left mouse button on windows tilebar
        private bool DragWindow = false;

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

        //removes tag from filter list when clicked
        private void ActiveTag_Click(object sender, RoutedEventArgs e)
        {
            if((Tag)CurrentTagList.SelectedItem != null) MainViewModel.FilterHandler.RemoveTagFilter((Tag)CurrentTagList.SelectedItem);
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
                    if(MainViewModel.CurrentData.AllFiles.All(p => p.Path != path))
                    {
                    MyFile pdf = new MyFile(MainViewModel) { Path = path, Title = System.IO.Path.GetFileNameWithoutExtension(path)};
                        MainViewModel.CurrentData.AllFiles.Add(pdf);
                        CoverArtGenerator.ConvertPDF(pdf, MainViewModel.CurrentData.Folder);
                    }
                }
                MainViewModel.Reset();
            }         
        }

        private void TagNameTextBlock_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return && TagCreation.Visibility == Visibility.Visible)
            {
                CreateTagBtn_Click(sender,e);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MainViewModel.CurrentData.SaveFilesToFile();
            MainViewModel.CurrentData.SaveTagsToFile();
        }

        private void CreateTagBtn_Click(object sender, RoutedEventArgs e)
        {
            Tag newtag = new Tag(MainViewModel.CurrentData.AllTags) { Content = TagNameTextBlock.Text, BackgroundColor = (Color)ColorSelector.SelectedColor};
            if (ParentSelectionTree.SelectedItem != null)
            {
                Tag Parent = ParentSelectionTree.SelectedItem as Tag;
                Parent.Items.Add(newtag);
                newtag.ParentID = Parent.ID;
            }
            else
            {
                MainViewModel.CurrentData.RootTags.Add(newtag);
            }

            MainViewModel.CurrentData.AllTags.Add(newtag);
            MainViewModel.TFViewModel.TreeViewSource = MainViewModel.TFViewModel.CreateTreeViewSourceFromCollection(MainViewModel.CurrentData.RootTags);
            MainViewModel.TFViewModel.AllTreeViewNodes = MainViewModel.TFViewModel.CreateAllTreeViewNodes(MainViewModel.TFViewModel.TreeViewSource);
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

        #region Windows Tile Bar Buttons
                private void MinimizeWindow(object sender, RoutedEventArgs e)
                {
                    App.Current.MainWindow.WindowState = WindowState.Minimized;
                }
                private void WindowsBar_MouseDown(object sender, MouseButtonEventArgs e)
                {
                    if (e.ClickCount == 2)
                    {
                        WindowState = WindowState.Maximized;
                        DragWindow = false;
                    }

                    else
                    {
                        DragMove();
                        if (WindowState == WindowState.Maximized) DragWindow = WindowState == WindowState.Maximized;
                    }
                }
                private void OnMouseMove(object sender, MouseEventArgs e)
                {
                    if (DragWindow)
                    {
                        DragWindow = false;

                        var point = e.MouseDevice.GetPosition(this);

                        Left = point.X - (RestoreBounds.Width * 0.5);
                        Top = point.Y - 20;

                        WindowState = WindowState.Normal;

                        try
                        {
                            DragMove();
                        }

                        catch (InvalidOperationException)
                        {
                            WindowState = WindowState.Maximized;
                        }
                   
                    }
                }
                private void CloseButton_Click(object sender, RoutedEventArgs e)
                {
                    this.Close();
                }
        #endregion

        private void ClearParent_Click(object sender, RoutedEventArgs e)
        {
            //ClearTreeViewSelection(ParentSelectionTree);
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


