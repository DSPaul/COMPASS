using Avalonia.Platform.Storage;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;

namespace COMPASS.Tests.Common.Mocks
{
    public class MockFilesService : IFilesService
    {
        public async Task<IList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions? options = null) => [];

        public async Task<IList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions? options = null) => [];

        public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions? options = null)
        {
            string extension = options?.DefaultExtension
                ?? options?.FileTypeChoices
                    ?.FirstOrDefault()
                    ?.Patterns
                    ?.FirstOrDefault()
                    ?.TrimStart('*')
                ?? string.Empty;

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
            File.WriteAllBytes(tempFilePath, []);

            var window = new Avalonia.Controls.Window();
            window.Show();
            return await window.StorageProvider.TryGetFileFromPathAsync(tempFilePath);
        }

        public FilePickerFileType SatchelExtensionFilter =>
            new("COMPASS Satchel File")
            {
                Patterns = [$"*{Constants.SatchelExtension}"]
            };


        public FilePickerFileType ZipExtensionFilter =>
            new("Zip file")
            {
                Patterns = [$"*.zip"]
            };
    }
}
