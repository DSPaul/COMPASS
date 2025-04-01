using System.Collections.Generic;
using System.Threading.Tasks;
using COMPASS.Common.Models;
using SharpCompress.Archives.Zip;

namespace COMPASS.Common.Interfaces.Storage;

public interface ICodexCollectionStorageService
{
    void EnsureDirectoryExists();
    
    IList<CodexCollection> GetAllCollections();

    Task AllocateNewCollection(CodexCollection newCollection);
    
    #region Load
    
    int Load(CodexCollection collection);
    bool LoadCodices(CodexCollection collection);
    bool LoadTags(CodexCollection collection);
    bool LoadInfo(CodexCollection collection);

    #endregion
    
    #region Save
    bool Save(CodexCollection collection);
    bool SaveCodices(CodexCollection collection);
    bool SaveTags(CodexCollection collection);
    bool SaveInfo(CodexCollection collection);
    
    #endregion
    
    #region Import
    
    Task<CodexCollection?> OpenSatchel(string? satchelPath = null);
    
    #endregion

    #region Export

    Task ExportTags(CodexCollection collection);
    
    void AddCollectionToArchive(ZipArchive archive, CodexCollection collection);

    void CompressUserDataToZip(string zipPath);
    
    #endregion
    
    void OnCollectionRenamed(string oldname, string newName);
    void OnCollectionDeleted(CodexCollection collection);
}