using COMPASS.Windows;
using Microsoft.Win32;

namespace COMPASS.ViewModels
{
    public class FileSourceViewModel : LocalSourceViewModel
    {
        public override Sources Source => Sources.File;

        public override void Import()
        {
            OpenFileDialog openFileDialog = new()
            {
                AddExtension = false,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProgressCounter = 0;
                ImportAmount = openFileDialog.FileNames.Length; ;

                ProgressWindow window = GetProgressWindow();
                window.Show();

                InitWorker(ImportFilePaths);
                worker.RunWorkerAsync(argument: openFileDialog.FileNames);
            }
        }
    }
}
