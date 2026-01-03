using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Interfaces;

namespace COMPASS.Common.ViewModels.ModelVMs;

public class TagViewModel : ModelViewModelBase<Tag>, IHasChildren<TagViewModel>
{
    private readonly CodexCollectionVM _codexCollectionVm;
    
    public TagViewModel(Tag tag, CodexCollectionVM codexCollectionVm) : base(tag)
    {
        _codexCollectionVm =  codexCollectionVm;
        
        _derivedProperties.Add(nameof(Tag.Name), [nameof(LongName), nameof(CalculatedLinkedGlobs)]);
        _derivedProperties.Add(nameof(Tag.InternalBackgroundColor), [nameof(BackgroundColor)]);
        _derivedProperties.Add(nameof(Tag.Parent), [nameof(BackgroundColor)]);
        
        AddValidation(nameof(Name), ValidateName);
    }
    
    #region Properties
    
    public TagViewModel? Parent
    {
        get => _model.Parent == null ? null : _codexCollectionVm.GetTagVm(_model.Parent);
        set => _model.Parent = value?._model;
    }
    
    public RangeObservableCollection<TagViewModel> Children => new (_model.Children.Select(_codexCollectionVm.GetTagVm));
    
    public string Name
    {
        get => _model.Name;
        set => _model.Name = value;
    }

    public string LongName => $"{Parent?.LongName}{(Parent == null ? "" : " > ")}{Name}";

    //Color bound to the UI
    public Color BackgroundColor => _model.BackgroundColor;

    //Internally stored color, can be null to indicate it should follow the color of the parent tag
    private Color? _internalBackgroundColor;
    public Color? InternalBackgroundColor
    {
        get => _internalBackgroundColor;
        set => SetProperty(ref _internalBackgroundColor, value);
    }
    
    public int Id => _model.Id;

    public bool IsGroup
    {
        get => _model.IsGroup;
        set => _model.IsGroup = value;
    }
    
    
    public ObservableCollection<string> LinkedGlobs => _model.LinkedGlobs;
    public List<string> CalculatedLinkedGlobs => PreferencesService.GetInstance().Preferences.AutoLinkFolderTagSameName ? [$"**/{Name}/**"] : [];
    
    #endregion Properties
    
    #region Validation

    private void ValidateName()
    {
        if (string.IsNullOrEmpty(_model.Name))
        {
            AddError(nameof(_model.Name), "Name is required.");
        }

        var siblings = ActiveCollection.AllTags.Where(t => t.Id != _model.Id).Where(tag => tag.Parent == _model.Parent);
        if (siblings.Any(tag => tag.Name == _model.Name))
        {
            AddError(nameof(Name), "Name must be unique within its parent.");
        }
    }
    #endregion
}