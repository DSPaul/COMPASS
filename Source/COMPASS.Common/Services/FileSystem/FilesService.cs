using Avalonia.Controls;
using Avalonia.Platform.Storage;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.Common.Services.FileSystem
{
    internal class FilesService : IFilesService
    {
        private readonly Window _target;

        public FilesService(Window target)
        {
            _target = target;
        }

        public async Task<IList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions? options = null)
        {
            options ??= new FilePickerOpenOptions();
            var files = await _target.StorageProvider.OpenFilePickerAsync(options);
            return files.ToList();
        }

        public async Task<IList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions? options = null)
        {
            options ??= new FolderPickerOpenOptions();
            var folders = await _target.StorageProvider.OpenFolderPickerAsync(options);
            return folders.ToList();
        }

        public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions? options = null)
        {
            options ??= new FilePickerSaveOptions();
            return await _target.StorageProvider.SaveFilePickerAsync(options);
        }

        public List<FilePickerFileType> SatchelExtensionFilter =>
            [
                new ("COMPASS Satchel File")
                {
                    Patterns = [$"*.{Constants.SatchelExtension}"]
                }
            ];
    }
}
