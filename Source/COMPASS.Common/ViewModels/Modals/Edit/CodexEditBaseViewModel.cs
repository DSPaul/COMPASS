using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Tools;

namespace COMPASS.Common.ViewModels.Modals.Edit;

public abstract class CodexEditBaseViewModel: ViewModelBase, IModalViewModel, IConfirmable
{
    public CodexEditBaseViewModel()
    {
        PublisherOptions = ["", ..MainViewModel.CollectionVM.FilterVM.PublisherList];
    }

    #region Properties
    
    protected ObservableCollection<TreeNode>? _treeViewSource;
    public ObservableCollection<TreeNode> AllTagsAsTreeNodes => _treeViewSource ??= 
        new(MainViewModel.CollectionVM.CurrentCollection.RootTags.Select(tag => new TreeNode(tag)));

    protected HashSet<TreeNode> AllTreeNodes => AllTagsAsTreeNodes.Flatten().ToHashSet();
    
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
    public abstract int? WindowWidth { get; }
    public abstract int? WindowHeight { get; }
    public Action CloseAction { get; set; } = () => { };

    #endregion
}