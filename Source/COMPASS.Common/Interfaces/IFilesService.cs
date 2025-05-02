using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace COMPASS.Common.Interfaces
{
    public interface IFilesService
    {
        Task<IList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions? options = null);
        Task<IList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions? options = null);
        Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions? options = null);

        FilePickerFileType SatchelExtensionFilter { get; }
        FilePickerFileType ZipExtensionFilter { get; }
    }
}
