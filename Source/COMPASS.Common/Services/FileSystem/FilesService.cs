using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;

namespace COMPASS.Common.Services.FileSystem
{
    internal class FilesService : IFilesService
    {
        public async Task<IList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions? options = null)
        {
            options ??= new FilePickerOpenOptions();
            var files = await App.MainWindow.StorageProvider.OpenFilePickerAsync(options);
            return files.ToList();
        }

        public async Task<IList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions? options = null)
        {
            options ??= new FolderPickerOpenOptions();
            var folders = await App.MainWindow.StorageProvider.OpenFolderPickerAsync(options);
            return folders.ToList();
        }

        public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions? options = null)
        {
            options ??= new FilePickerSaveOptions();
            return await App.MainWindow.StorageProvider.SaveFilePickerAsync(options);
        }

        public FilePickerFileType SatchelExtensionFilter =>
            new("COMPASS Satchel File")
            {
                Patterns = [$"*.{Constants.SatchelExtension}"]
            };


        public FilePickerFileType ZipExtensionFilter =>
            new("Zip file")
            {
                Patterns = [$"*.zip"]
            };
    }
}
