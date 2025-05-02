using System.Collections.Generic;
using System.Linq;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Tools;

namespace COMPASS.Common.ViewModels.Selection;

public class HierarchicalSelectorViewmodel<T> : ViewModelBase where T : class, IHasChildren<T>
{
    private IList<CheckableTreeNode<T>> _optionsRoot;

    public HierarchicalSelectorViewmodel(IList<T> options)
    {
        _optionsRoot = options.Select(x => new CheckableTreeNode<T>(x)).ToList();
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
    
    public IList<T> SelectedOptions => CheckableTreeNode<T>.GetCheckedItems(OptionsRoot).ToList();
    public int SelectedOptionsCount => SelectedOptions.Flatten().Count();
    
    /// <summary>
    /// A flat list of all options that are not selected.
    /// </summary>
    public IList<T> UncheckedOptions => OptionsRoot.Flatten().Where(x => x.IsChecked == false).Select(x => x.Item).ToList();

    private void OnSelectionChanged(bool? newValue)
    {
        OnPropertyChanged(nameof(SelectedOptions));
        OnPropertyChanged(nameof(SelectedOptionsCount));
    }
}