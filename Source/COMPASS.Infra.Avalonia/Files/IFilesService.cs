using Avalonia.Platform.Storage;
namespace COMPASS.Infra.Avalonia.Files
{
    public interface IFilesService
    {
        Task<IList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions? options = null);
        Task<IList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions? options = null);
        Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions? options = null);
        FilePickerFileType ZipExtensionFilter { get; }
    }
}
