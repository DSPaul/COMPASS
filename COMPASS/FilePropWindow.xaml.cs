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
        public MyFile Tempfile = new MyFile();

        public FilePropWindow()
        {
            InitializeComponent();
            Tempfile.Copy(Data.EditedFile);
            this.DataContext = Tempfile;
            TagSelection.DataContext = Data.RootTags;

            FileAuthorTB.ItemsSource = Data.AuthorList.OrderBy(n => n);

            foreach (Tag t in Data.AllTags)
            {
                if (Tempfile.Tags.Contains(t))
                {
                    t.Check = true;
                }
                else
                {
                    t.Check = false;
                }
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
            if (TagSelection.Visibility == Visibility.Collapsed)
            {
                TagSelection.Visibility = Visibility.Visible;
                ManageTagsBtn.Content = "Hide";
            }
            else
            {
                TagSelection.Visibility = Visibility.Collapsed;
                ManageTagsBtn.Content = "Manage";
            }
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            Update_Taglist();
            foreach (Tag t in Data.AllTags) t.Check = false;
            Data.EditedFile.Copy(Tempfile);
            if(FileAuthorTB.Text != "" && !Data.AuthorList.Contains(FileAuthorTB.Text))
            {
                Data.AuthorList.Add(FileAuthorTB.Text);
            }               
            MainWindow.Update_ActiveFiles();
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
            Data.AllFiles.Remove(Data.EditedFile);
            Data.TagFilteredFiles.Clear();
            Data.SearchFilteredFiles.Clear();
            foreach (MyFile f in Data.AllFiles)
            {
                Data.SearchFilteredFiles.Add(f);
                Data.TagFilteredFiles.Add(f);
            }
            MainWindow.Update_ActiveFiles();
            CoverIm.Source = null;
            //File.Delete(Tempfile.CoverArt);
            this.Close();
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainGrid.Focus();
        }
    }
}
