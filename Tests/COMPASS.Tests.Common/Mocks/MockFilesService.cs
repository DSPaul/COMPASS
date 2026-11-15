using Avalonia.Platform.Storage;
using COMPASS.Infra.Avalonia.Files;

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

        public Task<IStorageFolder?> TryGetFolderFromPathAsync(string folderPath) => Task.FromResult<IStorageFolder?>(null);

        //Pure sorting with no picker or window involved, so the real implementation is the right one here
        public void ClassifyDroppedFiles(IEnumerable<IStorageItem> droppedItems, out List<string> filesOnDisk, out List<IStorageFile> filesFromStream) =>
            new FilesService().ClassifyDroppedFiles(droppedItems, out filesOnDisk, out filesFromStream);

        public FilePickerFileType ZipExtensionFilter =>
            new("Zip file")
            {
                Patterns = [$"*.zip"]
            };
    }
}
