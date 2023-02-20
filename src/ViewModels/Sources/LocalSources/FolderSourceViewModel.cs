using COMPASS.Windows;
using Ookii.Dialogs.Wpf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace COMPASS.ViewModels
{
    public class FolderSourceViewModel : LocalSourceViewModel
    {
        public override Sources Source => Sources.Folder;

        public override void Import()
        {
            VistaFolderBrowserDialog openFolderDialog = new();

            var dialogresult = openFolderDialog.ShowDialog();
            if (dialogresult == false) return;

            //find files in folder, including subfolder
            List<string> toSearch = new(openFolderDialog.SelectedPaths); //list with folders to search
            List<string> toImport = new(); //list with files to import

            while (toSearch.Count > 0)
            {
                string currentFolder = toSearch[0];
                toSearch.AddRange(Directory.GetDirectories(currentFolder));
                toImport.AddRange(Directory.GetFiles(currentFolder));
                toSearch.Remove(currentFolder);
            }

            //find how many files of each filetype
            var toImport_grouped = toImport.GroupBy(Path.GetExtension);
            ToImportFiletypes = toImport_grouped.Select(x => new FileTypeInfo(x.Key, x.Count(), true)).ToList();

            //open window to let user choose which filetypes to import
            ImportFolderWindow importFolderWindow;

            importFolderWindow = new(this)
            {
                Owner = Application.Current.MainWindow
            };

            dialogresult = importFolderWindow.ShowDialog();
            if (dialogresult == false) return;

            //Make new toImport with only selected Filetypes
            toImport = new List<string>();
            foreach (var filetypeHelper in ToImportFiletypes)
            {
                if (filetypeHelper.ShouldImport)
                {
                    toImport.AddRange(toImport_grouped.First(g => g.Key == filetypeHelper.FileExtension));
                }
            }

            ProgressCounter = 0;
            ImportAmount = toImport.Count;

            ProgressWindow window = GetProgressWindow();
            window.Show();

            InitWorker(ImportFilePaths);
            worker.RunWorkerAsync(argument: toImport);
        }

        #region File Type Selection Window stuff
        private IEnumerable<FileTypeInfo> _toImportFiletypes;
        public IEnumerable<FileTypeInfo> ToImportFiletypes
        {
            get => _toImportFiletypes;
            set => SetProperty(ref _toImportFiletypes, value);
        }

        //helper class for file type selection during folder import
        public class FileTypeInfo
        {
            public FileTypeInfo(string extension, int fileCount, bool shouldImport)
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
