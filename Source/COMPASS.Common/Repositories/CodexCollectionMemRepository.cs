using COMPASS.Common.Interfaces.Repos;
using COMPASS.Common.Models;

namespace COMPASS.Common.Repositories
{
    /// <summary>
    /// A repo for temporary in-memory codex collections
    /// </summary>
    public class CodexCollectionMemRepository : ICodexCollectionRepository
    {
        public void Init()
        {
            //Nothing to init
        }

        #region Create 
        public void AllocateNewCollection(CodexCollection collection) { }
        #endregion

        #region Read
        public IList<CodexCollection> GetAllCollections() => [];

        #region Load Data From File

        public int Load(CodexCollection collection) => 0;

        public void Unload(CodexCollection collection)
        {
            
        }

        public bool LoadTags(CodexCollection collection) => true;

        public bool LoadCodices(CodexCollection collection) => true;

        public bool LoadInfo(CodexCollection collection) => true;

        #endregion
        #endregion

        #region Update
        #region Save Data To XML File

        public bool Save(CodexCollection collection) => true;

        public bool SaveTags(CodexCollection collection, Stream? stream)
        {
            if(stream != null)
            {
                // No format defined to save to stream
                throw new InvalidOperationException();
            }

            return true;
        }

        public bool SaveCodices(CodexCollection collection, Stream? stream)
        {
            if (stream != null)
            {
                // No format defined to save to stream
                throw new InvalidOperationException();
            }

            return true;
        }

        public bool SaveInfo(CodexCollection collection, Stream? stream)
        {
            if (stream != null)
            {
                // No format defined to save to stream
                throw new InvalidOperationException();
            }

            return true;
        }

        #endregion

        public void OnCollectionRenamed(string oldName, string newName)
        {
           
        }

        #endregion

        #region Delete

        public void DeleteCollection(string collectionId)
        {
            
        }

        #endregion
    }
}
