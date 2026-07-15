using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.Models.Interfaces;

namespace COMPASS.Common.ViewModels.Modals.Edit;

public abstract class EditViewModelBase<TViewModel, TModel> : ViewModelBase, IConfirmable, IModalViewModel, IDisposable 
    where TViewModel : ModelViewModelBase<TModel>
    where TModel : ObservableObject, ICloneable<TModel>
{
    /// <summary>
    /// Create an edit viewmodel to edit or create 1 object
    /// </summary>
    /// <param name="source"> Either an object to edit, or a template in case of createNew </param>
    /// <param name="createNew"> Indicates that a new object should be created, rather than edit an existing object </param>
    /// <param name="createViewModel"> A method to create a viewmodel from a model </param>
    public EditViewModelBase(TModel source, bool createNew, Func<TModel, TViewModel> createViewModel)
    {
        _source = source;
        _createNew =  createNew;
        _createViewModel = createViewModel;

        _workingCopy = createViewModel(source.Clone());
        _workingCopy.PropertyChanged += HandleWorkingCopyPropertyChanged;

        //Validate model immediatly because can be invalid from the start
        WorkingCopy.Validate();
    }
    
    protected readonly TModel _source;
    protected readonly bool _createNew;
    private readonly Func<TModel, TViewModel> _createViewModel;
    
    #region Properties
    
    //Temporary copy to work with
    private TViewModel? _workingCopy;
    public TViewModel WorkingCopy
    {
        get => _workingCopy!;
        private set
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
    
    protected abstract void HandleCreateNew(TModel newObj);
    protected abstract void BeforeApply(TModel source, TModel proposal);
    protected abstract void OnApplied(TModel source);
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
        WorkingCopy = _createViewModel(_source.Clone());
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
            return !hasErrorInfo.HasErrors;
        }
        
        return true;
    }

    protected virtual void Confirm()
    {
        TModel model = WorkingCopy.GetModel();
        
        //Apply changes 
        if (_createNew)
        {
            TModel newObj = model.Clone();
            HandleCreateNew(newObj);
        }
        else
        {
            BeforeApply(_source, model);
            _source.CopyFrom(model);
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