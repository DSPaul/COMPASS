using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace COMPASS.Common.Interfaces
{
    internal interface IFilesService
    {
        Task<IList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions? options = null);
        Task<IList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions? options = null);
        Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions? options = null);

        List<FilePickerFileType> SatchelExtensionFilter { get; }
    }
}
