using COMPASS.Common.Models;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.ViewModels.Modals.Edit;

public class CollectionEditViewModel : EditViewModelBase<CollectionInfoViewModel, CollectionInfo>
{
    private readonly CodexCollectionVM _collectionVm;

    public CollectionEditViewModel(CodexCollectionVM collectionVm, bool createNew)
        : base(
            createNew ? new CollectionInfo() : collectionVm.Collection.Info,
            createNew,
            model => new CollectionInfoViewModel(model))
    {
        _collectionVm = collectionVm;
        AddValidation(nameof(Name), ValidateName);

        Name = createNew ? "New collection" : _collectionVm.Collection.Name;
    }

    #region Properties

    public string Name
    {
        get;
        set
        {
            if(SetProperty(ref field, value))
            {
                ValidateName();
            }
        }
    }

    #endregion

    #region Abstract implementations

    protected override async void HandleCreateNew(CollectionInfo newCollectionInfo) 
    {
        CollectionHandle? newHandle = CollectionManager.CreateAndLoadCollection(Name);
        newHandle?.CollectionVM.Collection.Info = newCollectionInfo;

        if (newHandle != null)
        {
            var activeTab = TabsViewModel.GetInstance().ActiveTab;
            if (activeTab != null)
            {
                await activeTab.ChangeToCollection(newHandle);
            }
            else
            {
                var tab = TabsViewModel.GetInstance().CreateTab();
                await tab.ChangeToCollection(newHandle);
            }
        }
    }

    protected override void BeforeApply(CollectionInfo source, CollectionInfo proposal)
    {

    }

    protected override void OnApplied(CollectionInfo source)
    {
        if(_collectionVm.Collection.Name != Name)
        {
            _collectionVm.RenameCollection(Name);
        }
    }

    #endregion

    private void ValidateName()
    {
        var otherCollections = CollectionManager.CollectionVms.Except([_collectionVm]).ToList();
        if (!CollectionManager.IsValidCollectionName(Name, out string? reason, otherCollections))
        {
            AddError(nameof(Name), reason!);
        }
    }

    #region IModalViewModel

    public override string WindowTitle => _createNew ? "Create a new collection" : "Edit collection";

    #endregion
}
