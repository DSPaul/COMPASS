using System;
using COMPASS.Common.Interfaces.ViewModels;

namespace COMPASS.Common.ViewModels.Main;

public class CollectionHandle: IDisposable
{
    private readonly CodexCollectionVM _collectionVm;
    private bool _disposed = false;

    public CollectionHandle(CodexCollectionVM collectionVm)
    {
        _collectionVm = collectionVm;
    }

    public CodexCollectionVM CollectionVM => _collectionVm;

    public void Save()
    {
        CollectionVM.Save(this);
    }
    
    public void SaveCodices()
    {
        CollectionVM.SaveCodices(this);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _collectionVm?.Unload(this);
            _disposed = true;
        }
    }
}
