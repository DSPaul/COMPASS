using COMPASS.Commands;
using COMPASS.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace COMPASS.ViewModels
{
    public class CollectionViewModel : ObservableObject
    {
        public CollectionViewModel()
        {
            LoadInitialCollection();
        }

        #region Properties

        private ObservableCollection<string> _collectionDirectories;
        public ObservableCollection<string> CollectionDirectories
        {
            get => _collectionDirectories;
            set => SetProperty(ref _collectionDirectories, value);
        }

        private string _currentCollectionName;
        public string CurrentCollectionName
        {
            get => _currentCollectionName;
            set
            {
                if (CurrentCollection != null)
                {
                    CurrentCollection.SaveCodices();
                    CurrentCollection.SaveTags();
                }
                if (value != null) ChangeCollection(value);
                SetProperty(ref _currentCollectionName, value);
            }
        }

        private CodexCollection _currentCollection;
        public CodexCollection CurrentCollection
        {
            get => _currentCollection;
            set => SetProperty(ref _currentCollection, value);
        }

        private FilterViewModel _filterVM;
        public FilterViewModel FilterVM
        {
            get => _filterVM;
            private set => SetProperty(ref _filterVM, value);
        }

        private TagsViewModel _tagsVM;
        public TagsViewModel TagsVM
        {
            get => _tagsVM;
            set => SetProperty(ref _tagsVM, value);
        }

        #endregion

        #region Methods and Commands
        private void LoadInitialCollection()
        {
            Directory.CreateDirectory(CodexCollection.CollectionsPath);

            //Get all collections by folder name

            string[] FullPathFolders = Directory.GetDirectories(CodexCollection.CollectionsPath);
            CollectionDirectories = new(FullPathFolders.Select(path => Path.GetFileName(path)));

            //in case of first boot, create default folder
            if (CollectionDirectories.Count == 0)
            {
                CreateCollection("Default");
            }

            //in case startup collection no longer exists, pick first one that does exists
            else if (!Directory.Exists(CodexCollection.CollectionsPath + Properties.Settings.Default.StartupCollection))
            {
                MessageBox.Show("The collection " + Properties.Settings.Default.StartupCollection + " could not be found. ");
                CurrentCollectionName = CollectionDirectories.First(dir => Directory.Exists(CodexCollection.CollectionsPath + dir));
            }

            //otherwise, open startup collection
            else
            {
                CurrentCollectionName = Properties.Settings.Default.StartupCollection;
            }
        }

        // Change Collection
        public void ChangeCollection(string collectionDir)
        {
            CurrentCollection = new(collectionDir);
            FilterVM = new(CurrentCollection.AllCodices);
            TagsVM = new(this);
        }

        // Create CodexCollection
        private RelayCommand<string> _createCollectionCommand;
        public RelayCommand<string> CreateCollectionCommand => _createCollectionCommand ??= new(CreateCollection);
        public void CreateCollection(string dirName)
        {
            if (string.IsNullOrEmpty(dirName)) return;

            Directory.CreateDirectory(CodexCollection.CollectionsPath + dirName + @"\CoverArt");
            Directory.CreateDirectory(CodexCollection.CollectionsPath + dirName + @"\Thumbnails");
            CollectionDirectories.Add(dirName);
            CurrentCollectionName = dirName;
        }

        // Rename Collection
        private RelayCommand<string> _editCollectionNameCommand;
        public RelayCommand<string> EditCollectionNameCommand => _editCollectionNameCommand ??= new(EditCollectionName);
        public void EditCollectionName(string newName)
        {
            var index = CollectionDirectories.IndexOf(CurrentCollectionName);
            CurrentCollection.RenameCollection(newName);
            CollectionDirectories[index] = newName;
            CurrentCollectionName = newName;
        }

        // Delete Collection
        private ActionCommand _deleteCollectionCommand;
        public ActionCommand DeleteCollectionCommand => _deleteCollectionCommand ??= new(RaiseDeleteCollectionWarning);
        public void RaiseDeleteCollectionWarning()
        {
            if (CurrentCollection.AllCodices.Count > 0)
            {
                //MessageBox "Are you Sure?"
                string sCaption = "Are you Sure?";

                string MessageSingle = "There is still one file in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";
                string MessageMultiple = $"There are still {CurrentCollection.AllCodices.Count} files in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";

                string sMessageBoxText = CurrentCollection.AllCodices.Count == 1 ? MessageSingle : MessageMultiple;

                MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
                MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

                MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

                if (rsltMessageBox == MessageBoxResult.Yes)
                {
                    DeleteCollection(CurrentCollectionName);
                }
            }
            else
            {
                DeleteCollection(CurrentCollectionName);
            }
        }
        public void DeleteCollection(string todelete)
        {
            //if todelete is empty, it will delete the entire collections folder
            if (String.IsNullOrEmpty(todelete)) return;

            CollectionDirectories.Remove(todelete);
            CurrentCollectionName = CollectionDirectories.FirstOrDefault();
            Directory.Delete(CodexCollection.CollectionsPath + todelete, true);
        }
        #endregion
    }
}
