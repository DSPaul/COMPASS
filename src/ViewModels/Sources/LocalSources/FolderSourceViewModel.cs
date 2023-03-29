using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using Ookii.Dialogs.Wpf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace COMPASS.ViewModels.Sources
{
    public class FolderSourceViewModel : LocalSourceViewModel
    {
        public FolderSourceViewModel() : base() { }
        public FolderSourceViewModel(CodexCollection targetCollection) : base(targetCollection) { }

        public override ImportSource Source => ImportSource.Folder;
        public List<string> FolderNames { get; set; } = new();
        public List<string> FileNames { get; set; } = new();

        public override void Import()
        {
            IsImporting = true;

            VistaFolderBrowserDialog openFolderDialog = new()
            {
                Multiselect = true,
            };

            var dialogresult = openFolderDialog.ShowDialog();
            if (dialogresult == false) return;

            FolderNames = openFolderDialog.SelectedPaths.ToList();

            ImportFolders();
        }

        public void ImportFolders(bool hidden = false)
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

            //find how many files of each filetype
            var toImport_grouped = toImport.GroupBy(Path.GetExtension);

            //add new file extension to global file preferences
            foreach (string extension in toImport_grouped.Select(x => x.Key))
            {
                TargetCollection.Info.FiletypePreferences.TryAdd(extension, true);
            }

            if (!hidden)
            {
                //init ToImportFileTypes with values from FileTypePreferences
                ToImportFiletypes = toImport_grouped.Select(x => new FileTypeInfo(x.Key, TargetCollection.Info.FiletypePreferences[x.Key], x.Count())).ToList();

                //open window to let user choose which filetypes to import
                ImportFolderWindow importFolderWindow;

                importFolderWindow = new(this)
                {
                    Owner = Application.Current.MainWindow
                };

                var dialogresult = importFolderWindow.ShowDialog();
                if (dialogresult == false) return;

                //update the global file type preferences for the collection
                foreach (var filetypeHelper in ToImportFiletypes)
                {
                    TargetCollection.Info.FiletypePreferences[filetypeHelper.FileExtension] = filetypeHelper.ShouldImport;
                }
            }

            //Make new toImport with only selected Filetypes
            toImport = toImport.Where(path => TargetCollection.Info.FiletypePreferences[Path.GetExtension(path)]).ToList();
            ImportFiles(toImport, !hidden);
        }

        #region File Type Selection Window stuff
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
                    TargetCollection.Info.AutoImportDirectories.AddIfMissing(dir);
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
