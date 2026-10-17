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

        public async Task<IStorageFolder?> TryGetFolderFromPathAsync(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return null;
            }

            try
            {
                string fullPath = Path.GetFullPath(folderPath);

                return await WindowManager.MainWindow.StorageProvider
                    .TryGetFolderFromPathAsync(new Uri(fullPath)).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        public FilePickerFileType ZipExtensionFilter =>
            new("Zip file")
            {
                Patterns = [$"*.zip"]
            };
    }
}
