using System;
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
using System.Windows.Shapes;

namespace COMPASS
{
    /// <summary>
    /// Interaction logic for TagPropWindow.xaml
    /// </summary>
    public partial class TagPropWindow : Window
    {
        public Tag tempTag = new Tag();

        public TagPropWindow()
        {
            InitializeComponent();
            tempTag.Copy(Data.EditedTag);
            this.DataContext = tempTag;
            ParentSelection.DataContext = Data.RootTags;
            ShowParentSelectionBorder.DataContext = tempTag.GetParent();
            ColorSelector.SelectedColor = tempTag.BackgroundColor;
        }

        //makes objects lose focus when clicked away
        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainGrid.Focus();
        }

        //Refreshes all the files to apply changes in Tag
        private void UpdateAllFiles()
        {
            foreach (var f in Data.AllFiles.Where(f => f.Tags.Contains(tempTag)))
            {
                foreach (Tag t in Data.AllTags)
                {
                    if (f.Tags.Contains(t))
                    {
                        t.Check = true;
                    }
                    else
                    {
                        t.Check = false;
                    }
                }
                f.Tags.Clear();
                foreach (Tag t in Data.AllTags)
                {
                    if (t.Check)
                    {
                        f.Tags.Add(t);
                    }
                }
            }
        }

        private void ShowParentSelection_Click(object sender, RoutedEventArgs e)
        {
            if (ParentSelection.Visibility == Visibility.Collapsed)
            {
                ParentSelection.Visibility = Visibility.Visible;
                TagCreationMain.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (ParentSelectionTree.SelectedItem != null)
                {
                    Tag Parent = ParentSelectionTree.SelectedItem as Tag;
                    tempTag.ParentID = Parent.ID;
                    ShowParentSelectionBorder.DataContext = tempTag.GetParent();
                }
                ParentSelection.Visibility = Visibility.Collapsed;
                TagCreationMain.Visibility = Visibility.Visible;
            }
        }
        private void ClearParent_Click(object sender, RoutedEventArgs e)
        {
            tempTag.ParentID = -1;
            ShowParentSelectionBorder.DataContext = tempTag.GetParent();
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

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            //set BackgroundColor
            tempTag.BackgroundColor = (Color)ColorSelector.SelectedColor;

            //set Parent if changed
            if (Data.EditedTag.ParentID != tempTag.ParentID)
            {
                if(Data.EditedTag.ParentID == -1)
                {
                    Data.RootTags.Remove(Data.EditedTag);
                }
                else
                {
                    Data.EditedTag.GetParent().Items.Remove(tempTag);
                }
       
                if(tempTag.ParentID == -1)
                {
                    Data.RootTags.Add(Data.EditedTag);
                }
                else
                {
                    tempTag.GetParent().Items.Add(Data.EditedTag);
                }
            }

            //Apply changes 
            Data.EditedTag.Copy(tempTag);
            UpdateAllFiles();
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //make scroll with mousewheel work
        private void ParentSelection_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
