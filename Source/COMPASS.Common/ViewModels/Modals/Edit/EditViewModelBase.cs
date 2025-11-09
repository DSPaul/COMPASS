using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Infra.Models.Interfaces;

namespace COMPASS.Common.ViewModels.Modals.Edit;

public abstract class EditViewModelBase<T> : ViewModelBase, IConfirmable, IModalViewModel, IDisposable where T : ObservableObject, ICloneable<T>
{
    /// <summary>
    /// Create an edit viewmodel to edit or create 1 object
    /// </summary>
    /// <param name="source"> Either an object to edit, or a template in case of createNew </param>
    /// <param name="createNew"> Indicates that a new object should be created, rather than edit an existing object </param>
    public EditViewModelBase(T source, bool createNew)
    {
        _source = source;
        _createNew =  createNew;

        _workingCopy = source.Clone();
        _workingCopy.PropertyChanged += HandleWorkingCopyPropertyChanged;
    }
    
    protected readonly T _source;
    protected readonly bool _createNew;
    
    #region Properties
    
    //Temporary copy to work with
    private T? _workingCopy;
    public T WorkingCopy
    {
        get => _workingCopy!;
        protected set
        {
            if (_workingCopy != null)
            {
                _workingCopy.PropertyChanged -= HandleWorkingCopyPropertyChanged;
            }

            if (SetProperty(ref _workingCopy, value))
            {
                Validate();
            }
            
            _workingCopy.PropertyChanged += HandleWorkingCopyPropertyChanged;
        }
    }
    
    #endregion

    #region Abstract Methods

    /// <summary>
    /// Define optional validation
    /// </summary>
    protected abstract void HandleCreateNew(T newObj);
    protected abstract void BeforeApply(T source, T proposal);
    protected abstract void OnApplied(T source);
    #endregion

    #region Private methods

    private void HandleWorkingCopyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INotifyDataErrorInfo.HasErrors))
        {
            ConfirmCommand.NotifyCanExecuteChanged();
        }
    }

    protected virtual void Clear()
    {
        WorkingCopy = _source.Clone();
    }

    #endregion
    
    #region IConfirmable
    
    private RelayCommand? _cancelCommand;
    public IRelayCommand CancelCommand => _cancelCommand ??= new(Cancel);
    private void Cancel()
    {
        Clear();
        CloseAction();
    }
    
    private RelayCommand? _confirmCommand;
    public IRelayCommand ConfirmCommand => _confirmCommand ??= new(Confirm, CanConfirm);
    private bool CanConfirm()
    {
        if (WorkingCopy is INotifyDataErrorInfo hasErrorInfo)
        {
            return hasErrorInfo.HasErrors;
        }
        
        return true;
    }

    protected virtual void Confirm()
    {
        //Apply changes 
        if (_createNew)
        {
            T newObj = WorkingCopy.Clone();
            HandleCreateNew(newObj);
        }
        else
        {
            BeforeApply(_source, WorkingCopy);
            _source.CopyFrom(WorkingCopy);
            OnApplied(_source);
        }

        //reset fields
        Clear();
        CloseAction();
    }
    #endregion
    
    #region IModalViewModel
    public abstract string WindowTitle { get; }
    public Action CloseAction { get; set; } = () => { };
    #endregion

    #region  IDisposable
    
    public virtual void Dispose()
    {
        WorkingCopy.PropertyChanged -= HandleWorkingCopyPropertyChanged;
    }

    #endregion
    
}