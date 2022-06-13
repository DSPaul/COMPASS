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

namespace COMPASS
{
    /// <summary>
    /// Interaction logic for FilePropWindow.xaml
    /// </summary>
    public partial class FilePropWindow : Window
    {
        //Tempfile to save changes in, Only apply changes after "OK" is clicked
        public MyFile Tempfile = new MyFile();

        //Constructor
        public FilePropWindow()
        {
            InitializeComponent();
            //Make Tempfile a copy of the edited file
            Tempfile.Copy(Data.EditedFile);
            //Set Datacontexts and Itemsources
            this.DataContext = Tempfile;
            TagSelection.DataContext = Data.RootTags;
            FileAuthorTB.ItemsSource = Data.AuthorList.OrderBy(n => n);

            //Apply right checkboxes in Alltags
            foreach (Tag t in Data.AllTags)
            {
                t.Check = Tempfile.Tags.Contains(t)? true : false;
            }
        }

        private void BrowsepathBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AddExtension = false               
            };
            if (openFileDialog.ShowDialog() == true)
            {
                Tempfile.Path = openFileDialog.FileName;
            }
        }

        private void ManageTagsBtn_Click(object sender, RoutedEventArgs e)
        {
            //Display or hide TagTree
            TagSelection.Visibility = (TagSelection.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
            ManageTagsBtn.Content = (TagSelection.Visibility == Visibility.Collapsed) ? "Manage" : "Hide";
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            Update_Taglist();
            //Uncheck all Tags
            foreach (Tag t in Data.AllTags) t.Check = false;
            //Copy changes into Database
            Data.EditedFile.Copy(Tempfile);
            //Add new Author and Publishers to lists
            if(FileAuthorTB.Text != "" && !Data.AuthorList.Contains(FileAuthorTB.Text)) Data.AuthorList.Add(FileAuthorTB.Text);
            if (FilePublisherTB.Text != "" && !Data.PublisherList.Contains(FilePublisherTB.Text)) Data.PublisherList.Add(FilePublisherTB.Text);
            
            Data.Update_ActiveFiles();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Update_Taglist();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Update_Taglist();
        }

        public void Update_Taglist()
        {
            Tempfile.Tags.Clear();
            foreach (Tag t in Data.AllTags)
            {
                if (t.Check)
                {
                    Tempfile.Tags.Add(t);
                }
            }
        }

        private void DeleteFileBtn_Click(object sender, RoutedEventArgs e)
        {
            //Unload Image Before deleting
            //CoverIm.Source = null;
            //Delete
            Data.DeleteFile(Data.EditedFile);      
            //Close Window
            this.Close();
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainGrid.Focus();
        }
    }
}
