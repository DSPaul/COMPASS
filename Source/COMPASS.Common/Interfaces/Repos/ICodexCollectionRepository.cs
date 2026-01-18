using COMPASS.Common.Models;
namespace COMPASS.Common.Interfaces.Repos
{
    public interface ICodexCollectionRepository
    {
        void Init();

        #region Create 
        Task AllocateNewCollection(CodexCollection newCollection);
        #endregion

        #region Read
        IList<CodexCollection> GetAllCollections();
        
        int Load(CodexCollection collection);
        void Unload(CodexCollection collection);
        bool LoadCodices(CodexCollection collection);
        bool LoadTags(CodexCollection collection);
        bool LoadInfo(CodexCollection collection);
        #endregion


        #region Update
        /// <summary>
        /// Save the collection to the given stream, or to its default location if no stream is provided.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        bool Save(CodexCollection collection);
        bool SaveCodices(CodexCollection collection, Stream? stream = null);
        bool SaveTags(CodexCollection collection, Stream? stream = null);
        bool SaveInfo(CodexCollection collection, Stream? stream = null);

        void OnCollectionRenamed(string oldname, string newName);
        #endregion

        #region Delete
        void DeleteCollection(string collectionId);
        #endregion
    }
}
