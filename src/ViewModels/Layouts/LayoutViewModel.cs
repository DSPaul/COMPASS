using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace COMPASS.ViewModels
{
    public abstract class LayoutViewModel : ViewModelBase
    {
        public LayoutViewModel() : base()
        {
            GetSortOptions();
            SortOptionsMenuItem = new MyMenuItem("Sorty By")
            {
                Submenus = SortOptions
            };
        }

        #region Properties

        public CodexViewModel CodexVM { get; init; } = new();

        //Selected File
        private Codex _selectedFile;
        public Codex SelectedFile
        {
            get { return _selectedFile; }
            set { SetProperty(ref _selectedFile, value); }
        }

        //Set Type of view
        public Enums.CodexLayout LayoutType { get; init; }

        //list with options to costimize view
        private ObservableCollection<MyMenuItem> _viewOptions = new();
        public ObservableCollection<MyMenuItem> ViewOptions
        {
            get { return _viewOptions; }
            set { SetProperty(ref _viewOptions, value); }
        }

        //list with options to sort the files
        private MyMenuItem _sortOptionsMenuItem;
        public MyMenuItem SortOptionsMenuItem
        {
            get { return _sortOptionsMenuItem; }
            set { SetProperty(ref _sortOptionsMenuItem, value); }
        }

        //list with options to sort the files
        private ObservableCollection<MyMenuItem> _sortOptions;
        public ObservableCollection<MyMenuItem> SortOptions
        {
            get { return _sortOptions; }
            set { SetProperty(ref _sortOptions, value); }
        }
        #endregion

        private void GetSortOptions()
        {
            SortOptions = new ObservableCollection<MyMenuItem>();
            var SortNames = new List<(string, string)>()
            {
                //("Display name","Property Name")
                ("Title", "SortingTitle"),
                ("Author", "AuthorsAsString"),
                ("Publisher", "Publisher"),
                ("Release Date", "ReleaseDate"),
                ("User Rating", "Rating"),
                ("Page Count", "PageCount")
            };

            //double check on typos by checking if all property names exist in codex class
            var PossibleSortProptertyNames = typeof(Codex).GetProperties().Select(p => p.Name).ToList();
            if (SortNames.Select(pair => pair.Item2).Except(PossibleSortProptertyNames).Any())
            {
                MessageBox.Show("One of the sort property paths does not exist");
                Logger.log.Error("One of the sort property paths does not exist");
            }

            foreach (var sortOption in SortNames)
            {
                SortOptions.Add(new MyMenuItem(sortOption.Item1)
                {
                    Command = new RelayCommand<string>(MVM.CollectionVM.SortBy),
                    CommandParam = sortOption.Item2
                });
            }
        }

    }
}
