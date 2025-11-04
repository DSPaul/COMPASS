using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.ViewModels.Modals.Edit;

public abstract class CodexEditBaseViewModel: ViewModelBase, IModalViewModel, IConfirmable
{
    public CodexEditBaseViewModel(CollectionTabVM tabVm)
    {
        TabVM = tabVm;
        
        var publisherList = tabVm.FilterVM.PublisherList ?? [];
        PublisherOptions = ["", ..publisherList];
    }

    #region Properties
    
    public CollectionTabVM TabVM { get; }
    
    protected ObservableCollection<CheckableTreeNode<Tag>>? _allTagsAsTreeNodes;
    public ObservableCollection<CheckableTreeNode<Tag>> AllTagsAsTreeNodes => _allTagsAsTreeNodes ??= 
        new(TabVM.CollectionVM.Collection.RootTags.Select(tag => new CheckableTreeNode<Tag>(tag)));

    protected HashSet<CheckableTreeNode<Tag>> AllTreeNodes => AllTagsAsTreeNodes.Flatten().ToHashSet();
    
    public List<string> PublisherOptions { get; }

    #endregion

    #region IConfirmable
    
    private RelayCommand? _confirmCommand;
    public IRelayCommand ConfirmCommand => _confirmCommand ??= new(Confirm);
    protected abstract void Confirm();
    
    private RelayCommand? _cancelCommand;
    public IRelayCommand CancelCommand => _cancelCommand ??= new(Cancel);

    protected virtual void Cancel()
    {
        CloseAction();
    }

    #endregion

    #region IModalWindow
    
    public abstract string WindowTitle { get; }
    public Action CloseAction { get; set; } = () => { };

    #endregion
}