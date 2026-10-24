using COMPASS.Infra.Avalonia.Mvvm;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Common.Domain;
using COMPASS.Common.Features.Tabs;
using COMPASS.Infra.Collections;
using COMPASS.Infra.Text;

namespace COMPASS.Common.Features.CodexCollections;

public class CollectionEditViewModel : EditViewModelBase<CollectionInfoViewModel, CollectionInfo>
{
    private readonly CodexCollectionVM _collectionVm;
    private readonly CollectionTabVM _tabVm;
    private readonly CollectionManager _collectionManager;

    public CollectionEditViewModel(CollectionManager collectionManager, CodexCollectionVM collectionVm, CollectionTabVM tabVm, bool createNew)
        : base(
            createNew ? new CollectionInfo() : collectionVm.Collection.Info,
            createNew,
            model => new CollectionInfoViewModel(model))
    {
        _collectionManager = collectionManager;
        _collectionVm = collectionVm;
        _tabVm = tabVm;
        AddValidation(nameof(Name), ValidateName);

        Name = (createNew ? "New collection" : _collectionVm.Collection.Name)
            .GetValidVariant(name => IsNameValid(name, out _));
    }

    #region Properties

    public string Name
    {
        get;
        set
        {
            if(SetProperty(ref field, value))
            {
                Validate(nameof(Name));
                MarkAsTouched();
            }
        }
    }

    #endregion

    #region Abstract implementations

    protected override async void HandleCreateNew(CollectionInfo newCollectionInfo) 
    {
        CollectionHandle? newHandle = _collectionManager.CreateAndLoadCollection(Name);
        newHandle?.CollectionVM.Collection.Info = newCollectionInfo;

        if (newHandle != null)
        {
            await _tabVm.ChangeToCollection(newHandle);
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


    private bool IsNameValid(string name, out string? reason)
    {
        var otherCollections = _createNew ? 
            _collectionManager.CollectionVms:
            _collectionManager.CollectionVms.Without(_collectionVm);
        return CollectionManager.IsValidCollectionName(name, out reason, otherCollections);
    }

    private void ValidateName()
    {
        if (!IsNameValid(Name, out string? reason))
        {
            AddError(nameof(Name), reason!);
        }
    }

    #region IModalViewModel

    public override string WindowTitle => _createNew ? "Create a new collection" : "Edit collection";

    #endregion
}

[Factory]
public class CollectionEditViewModelFactory(CollectionManager collectionManager)
{
    /// <param name="tabVm">Tab that opens the new collection when <paramref name="createNew"/> is true</param>
    public CollectionEditViewModel Create(CodexCollectionVM collectionVm, CollectionTabVM tabVm, bool createNew)
        => new(collectionManager, collectionVm, tabVm, createNew);
}
