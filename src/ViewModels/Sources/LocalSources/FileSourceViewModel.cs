using Microsoft.Win32;
using System.Linq;

namespace COMPASS.ViewModels.Sources
{
    public class FileSourceViewModel : LocalSourceViewModel
    {
        public override ImportSource Source => ImportSource.File;

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
                ImportFiles(openFileDialog.FileNames.ToList(), true);
            }
        }
    }
}
