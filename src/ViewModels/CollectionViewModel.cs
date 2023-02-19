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

        private CodexCollection _currentCollection;
        public CodexCollection CurrentCollection
        {
            get => _currentCollection;
            set
            {
                if (_currentCollection != null)
                {
                    _currentCollection.SaveCodices();
                    _currentCollection.SaveTags();
                }

                if (value != null)
                {
                    value.Load();
                    SetProperty(ref _currentCollection, value);
                    FilterVM = new(value.AllCodices);
                    TagsVM = new(this);
                }
            }
        }

        private ObservableCollection<CodexCollection> _allCodexCollections;
        public ObservableCollection<CodexCollection> AllCodexCollections
        {
            get => _allCodexCollections;
            set => SetProperty(ref _allCodexCollections, value);
        }

        //Needed for binding to context menu "Move to Collection"
        public ObservableCollection<string> CollectionDirectories => new(AllCodexCollections.Select(collection => collection.DirectoryName));

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

        //show edit Collection Stuff
        private bool _createCollectionVisibility = false;
        public bool CreateCollectionVisibility
        {
            get => _createCollectionVisibility;
            set => SetProperty(ref _createCollectionVisibility, value);
        }

        //show edit Collection Stuff
        private bool _editCollectionVisibility = false;
        public bool EditCollectionVisibility
        {
            get => _editCollectionVisibility;
            set => SetProperty(ref _editCollectionVisibility, value);
        }

        #endregion

        #region Methods and Commands
        private void LoadInitialCollection()
        {
            Directory.CreateDirectory(CodexCollection.CollectionsPath);

            //Get all collections by folder name
            AllCodexCollections = new(Directory
                .GetDirectories(CodexCollection.CollectionsPath)
                .Select(Path.GetFileName)
                .Select(dir => new CodexCollection(dir)));

            //in case of first boot, create default folder
            if (AllCodexCollections.Count == 0)
            {
                CreateCollection("Default");
            }

            //in case startup collection no longer exists, pick first one that does exists
            else if (!AllCodexCollections.Any(collection => collection.DirectoryName == Properties.Settings.Default.StartupCollection))
            {
                MessageBox.Show("The collection " + Properties.Settings.Default.StartupCollection + " could not be found. ");
                CurrentCollection = AllCodexCollections.First();
            }

            //otherwise, open startup collection
            else
            {
                CurrentCollection = AllCodexCollections.First(collection => collection.DirectoryName == Properties.Settings.Default.StartupCollection);
            }
        }

        private bool isLegalCollectionName(string dirName)
        {
            bool legal =
                dirName.IndexOfAny(Path.GetInvalidPathChars()) < 0
                && dirName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && !AllCodexCollections.Any(collection => collection.DirectoryName == dirName)
                && !String.IsNullOrWhiteSpace(dirName)
                && dirName.Length < 100;
            return legal;
        }

        private ActionCommand _toggleCreateCollectionCommand;
        public ActionCommand ToggleCreateCollectionCommand => _toggleCreateCollectionCommand ??= new(ToggleCreateCollection);
        private void ToggleCreateCollection() => CreateCollectionVisibility = !CreateCollectionVisibility;

        private ActionCommand _toggleEditCollectionCommand;
        public ActionCommand ToggleEditCollectionCommand => _toggleEditCollectionCommand ??= new(ToggleEditCollection);
        private void ToggleEditCollection() => EditCollectionVisibility = !EditCollectionVisibility;

        // Create CodexCollection
        private RelayCommand<string> _createCollectionCommand;
        public RelayCommand<string> CreateCollectionCommand => _createCollectionCommand ??= new(CreateCollection, isLegalCollectionName);
        public void CreateCollection(string dirName)
        {
            if (string.IsNullOrEmpty(dirName)) return;

            CodexCollection newCollection = new(dirName);
            Directory.CreateDirectory(CodexCollection.CollectionsPath + dirName + @"\CoverArt");
            Directory.CreateDirectory(CodexCollection.CollectionsPath + dirName + @"\Thumbnails");
            AllCodexCollections.Add(newCollection);
            CurrentCollection = newCollection;

            CreateCollectionVisibility = false;
        }

        // Rename Collection
        private RelayCommand<string> _editCollectionNameCommand;
        public RelayCommand<string> EditCollectionNameCommand => _editCollectionNameCommand ??= new(EditCollectionName, isLegalCollectionName);
        public void EditCollectionName(string newName)
        {
            CurrentCollection.RenameCollection(newName);
            EditCollectionVisibility = false;
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
                    DeleteCollection(CurrentCollection);
                }
            }
            else
            {
                DeleteCollection(CurrentCollection);
            }
        }
        public void DeleteCollection(CodexCollection toDelete)
        {
            AllCodexCollections.Remove(CurrentCollection);
            CurrentCollection = AllCodexCollections.FirstOrDefault();

            //if Dir name of toDelete is empty, it will delete the entire collections folder
            if (String.IsNullOrEmpty(toDelete.DirectoryName)) return;
            Directory.Delete(CodexCollection.CollectionsPath + toDelete.DirectoryName, true);
        }
        #endregion
    }
}
