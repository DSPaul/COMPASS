using Avalonia.Platform.Storage;
using COMPASS.Infra.Avalonia.Modal;

namespace COMPASS.Infra.Avalonia.Files
{
    internal class FilesService : IFilesService
    {
        public async Task<IList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions? options = null)
        {
            options ??= new FilePickerOpenOptions();
            var files = await WindowManager.MainWindow.StorageProvider.OpenFilePickerAsync(options).ConfigureAwait(false);
            return files.ToList();
        }

        public async Task<IList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions? options = null)
        {
            options ??= new FolderPickerOpenOptions();
            var folders = await WindowManager.MainWindow.StorageProvider.OpenFolderPickerAsync(options).ConfigureAwait(false);
            return folders.ToList();
        }

        public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions? options = null)
        {
            options ??= new FilePickerSaveOptions();
            return await WindowManager.MainWindow.StorageProvider.SaveFilePickerAsync(options).ConfigureAwait(false);
        }

        public FilePickerFileType ZipExtensionFilter =>
            new("Zip file")
            {
                Patterns = [$"*.zip"]
            };
    }
}
