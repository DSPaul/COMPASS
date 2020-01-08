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
using COMPASS.Models;

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
            Tempfile.Copy(UserSettings.CurrentData.EditedFile);
            //Set Datacontexts and Itemsources
            this.DataContext = Tempfile;
            TagSelection.DataContext = UserSettings.CurrentData.RootTags;
            FileAuthorTB.ItemsSource = UserSettings.CurrentData.AuthorList.OrderBy(n => n);
            FilePublisherTB.ItemsSource = UserSettings.CurrentData.PublisherList.OrderBy(n => n);

            //Apply right checkboxes in Alltags
            foreach (Tag t in UserSettings.CurrentData.AllTags)
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
            foreach (Tag t in UserSettings.CurrentData.AllTags) t.Check = false;
            //Copy changes into Database
            UserSettings.CurrentData.EditedFile.Copy(Tempfile);
            //Add new Author and Publishers to lists
            if(FileAuthorTB.Text != "" && !UserSettings.CurrentData.AuthorList.Contains(FileAuthorTB.Text)) UserSettings.CurrentData.AuthorList.Add(FileAuthorTB.Text);
            if(FilePublisherTB.Text != "" && !UserSettings.CurrentData.PublisherList.Contains(FilePublisherTB.Text)) UserSettings.CurrentData.PublisherList.Add(FilePublisherTB.Text);
            
            UserSettings.CurrentData.Update_ActiveFiles();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
            foreach (Tag t in UserSettings.CurrentData.AllTags)
            {
                if (t.Check)
                {
                    Tempfile.Tags.Add(t);
                }
            }
        }

        private void DeleteFileBtn_Click(object sender, RoutedEventArgs e)
        {
            UserSettings.CurrentData.DeleteFile(UserSettings.CurrentData.EditedFile);
            Close();
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainGrid.Focus();
        }

        private void BrowseURLBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Tempfile.SourceURL);
        }
    }
}
