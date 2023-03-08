using COMPASS.Windows;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace COMPASS.ViewModels.Sources
{
    public class FileSourceViewModel : LocalSourceViewModel
    {
        public override ImportSource Source => ImportSource.File;
        public List<string> FileNames { get; set; }


        public override void Import()
        {
            IsImporting = true;

            OpenFileDialog openFileDialog = new()
            {
                AddExtension = false,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FileNames = openFileDialog.FileNames.ToList();
                StartAsyncImport();
            }
        }

        public void StartAsyncImport()
        {
            ProgressCounter = 0;
            ImportAmount = FileNames.Count;

            ProgressWindow window = GetProgressWindow();
            window.Show();

            InitWorker(ImportFilePaths);
            worker.RunWorkerAsync(argument: FileNames);
        }
    }
}
