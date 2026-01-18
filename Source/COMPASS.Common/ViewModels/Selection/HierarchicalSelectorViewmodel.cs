using COMPASS.Common.Models.Hierarchy;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Models.Interfaces;

namespace COMPASS.Common.ViewModels.Selection;

public class HierarchicalSelectorViewModel<T> : ViewModelBase where T : class, IHasChildren<T>
{
    private IList<CheckableTreeNode<T>> _optionsRoot;

    public HierarchicalSelectorViewModel(IList<T> options)
    {
        _optionsRoot = options.Select(x => new CheckableTreeNode<T>(x, propagateChanges:true)).ToList();
        TotalOptionsCount = _optionsRoot.Flatten().Count();
        
        foreach (var x in _optionsRoot)
        {
            x.Updated += OnSelectionChanged;
        }
    }

    public IList<CheckableTreeNode<T>> OptionsRoot
    {
        get => _optionsRoot;
        set => SetProperty(ref _optionsRoot, value);
    }
    public int TotalOptionsCount { get; }
    
    public IList<T> SelectedOptions => CheckableTreeNode.GetCheckedItems(OptionsRoot).ToList();
    public int SelectedOptionsCount => SelectedOptions.Flatten().Count();
    
    /// <summary>
    /// A flat list of all options that are not selected.
    /// </summary>
    public IList<T> UncheckedOptions => OptionsRoot.Flatten().Where(x => x.IsChecked == false).Select(x => x.Item).ToList();

    private void OnSelectionChanged(object? sender, bool? newValue)
    {
        OnPropertyChanged(nameof(SelectedOptions));
        OnPropertyChanged(nameof(SelectedOptionsCount));
    }
}