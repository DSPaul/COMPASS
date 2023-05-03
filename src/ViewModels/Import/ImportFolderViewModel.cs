using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using Ookii.Dialogs.Wpf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace COMPASS.ViewModels.Import
{
    public class ImportFolderViewModel : ObservableObject
    {
        public ImportFolderViewModel()
        {
            _targetCollection = MainViewModel.CollectionVM.CurrentCollection;
        }
        public ImportFolderViewModel(CodexCollection targetCollection)
        {
            _targetCollection = targetCollection;
        }

        private CodexCollection _targetCollection;

        public List<string> FolderNames { get; set; } = new();
        public List<string> FileNames { get; set; } = new();

        public List<string> ChooseFolders()
        {
            VistaFolderBrowserDialog openFolderDialog = new()
            {
                Multiselect = true,
            };

            var dialogresult = openFolderDialog.ShowDialog();
            if (dialogresult == false) return new();

            FolderNames = openFolderDialog.SelectedPaths.ToList();

            ImportViewModel.Stealth = false;
            return GetPathsFromFolders();
        }

        public List<string> GetPathsFromFolders()
        {
            //find files in folder, including subfolder
            List<string> toSearch = new(FolderNames); //list with folders to search
            List<string> toImport = new(FileNames); //list with files to import

            while (toSearch.Count > 0)
            {
                string currentFolder = toSearch[0];
                toSearch.AddRange(Directory.GetDirectories(currentFolder));
                toImport.AddRange(Directory.GetFiles(currentFolder));
                toSearch.Remove(currentFolder);
            }

            ImportAmount = toImport.Count;

            //find how many files of each filetype
            var toImport_grouped = toImport.GroupBy(Path.GetExtension);

            //add new file extension to global file preferences
            foreach (string extension in toImport_grouped.Select(x => x.Key))
            {
                _targetCollection.Info.FiletypePreferences.TryAdd(extension, true);
            }

            if (!ImportViewModel.Stealth)
            {
                //init ToImportFileTypes with values from FileTypePreferences
                ToImportFiletypes = toImport_grouped.Select(x => new FileTypeInfo(x.Key, _targetCollection.Info.FiletypePreferences[x.Key], x.Count())).ToList();

                //open window to let user choose which filetypes to import
                ImportFolderWindow importFolderWindow = new(this)
                {
                    Owner = Application.Current.MainWindow
                };

                var dialogresult = importFolderWindow.ShowDialog();
                if (dialogresult == false) return new();

                //update the global file type preferences for the collection
                foreach (var filetypeHelper in ToImportFiletypes)
                {
                    _targetCollection.Info.FiletypePreferences[filetypeHelper.FileExtension] = filetypeHelper.ShouldImport;
                }
            }

            //return toImport with only selected Filetypes
            return toImport.Where(path => _targetCollection.Info.FiletypePreferences[Path.GetExtension(path)]).ToList();
        }

        #region File Type Selection Window stuff
        public int ImportAmount { get; set; }

        private IEnumerable<FileTypeInfo> _toImportFiletypes;
        public IEnumerable<FileTypeInfo> ToImportFiletypes
        {
            get => _toImportFiletypes;
            set => SetProperty(ref _toImportFiletypes, value);
        }

        private RelayCommand<bool> _confirmImportCommand;
        public RelayCommand<bool> ConfirmImportCommand => _confirmImportCommand ??= new(ConfirmImport);
        public void ConfirmImport(bool isChecked)
        {
            if (isChecked)
            {
                foreach (string dir in FolderNames)
                {
                    _targetCollection.Info.AutoImportDirectories.AddIfMissing(dir);
                }
            }
        }

        //helper class for file type selection during folder import
        public class FileTypeInfo
        {
            public FileTypeInfo(string extension, bool shouldImport, int fileCount = 0)
            {
                FileExtension = extension;
                _fileCount = fileCount;
                ShouldImport = shouldImport;
            }

            private readonly int _fileCount;
            public string FileExtension { get; }
            public bool ShouldImport { get; set; }
            public string DisplayText => $"{FileExtension} ({_fileCount} file{(_fileCount > 1 ? @"s" : @"")})";
        }
        #endregion
    }
}
