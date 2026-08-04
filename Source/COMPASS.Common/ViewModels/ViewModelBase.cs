using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels
{
    public abstract class ViewModelBase : ObservableObject, INotifyDataErrorInfo
    {
        private ILogger? _logger;
        protected ILogger Logger => _logger ??= ServiceResolver.Resolve<ILogger>();

        /// <summary>
        /// Shortcut because we need this all over the place
        /// </summary>
        protected CodexCollection ActiveCollection
        {
            get
            {
                CollectionTabVM tabVm = TabsViewModel.GetInstance().ActiveTab 
                                        ?? throw new NoTabException("No collection is active because there is no active tab");
                return tabVm.CollectionVM.Collection;
            }
        }
        
        #region INotifyDataErrorInfo / Validation
        
        //list of errors per property
        private readonly Dictionary<string, List<string>> _errors = [];
        private readonly Dictionary<string, Action> _validationMethods = [];
        private readonly HashSet<string> _touchedProperties = [];

        // HasErrors considers ALL validation errors (including untouched properties) so CanConfirm is always accurate.
        public bool HasErrors => _errors.Any(kv => kv.Value.Count > 0);
    
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    
        public IEnumerable GetErrors(string? propertyName)
        {
            // Only return errors for properties the user has already touched,
            // so validation warnings don't appear before the user interacts with a field.
            if (string.IsNullOrEmpty(propertyName))
            {
                return _errors
                    .Where(kv => _touchedProperties.Contains(kv.Key))
                    .SelectMany(kv => kv.Value);
            }

            return !_touchedProperties.Contains(propertyName)
                ? Enumerable.Empty<string>()
                : (IEnumerable)(_errors.TryGetValue(propertyName, out var errors) ? errors : []);
        }

        protected void MarkAsTouched(string propertyName)
        {
            if (_touchedProperties.Add(propertyName))
            {
                // Fire ErrorsChanged so the UI immediately shows any existing error for this property.
                OnErrorsChanged(propertyName);
                OnPropertyChanged(nameof(HasErrors));
            }
        }
    
        public void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
            {
                _errors[propertyName] = [];
            }
        
            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
                OnPropertyChanged(nameof(HasErrors));
            }
        }
    
        public void ClearErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                var propertyNames = _errors.Keys.ToList();
                _errors.Clear();
        
                foreach (var propName in propertyNames)
                {
                    OnErrorsChanged(propName);
                }
            }
            else if (_errors.Remove(propertyName))
            {
                OnErrorsChanged(propertyName);
            }
        
            OnPropertyChanged(nameof(HasErrors));
        }
    
        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new(propertyName));
        }
        
        public void Validate(string? propertyName = null)
        {
            ClearErrors(propertyName);

            //validate
            if (propertyName == null)
            {
                foreach (var validationMethod in _validationMethods.Values)
                {
                    validationMethod.Invoke();
                }
            }
            else if(_validationMethods.TryGetValue(propertyName, out var validationMethod))
            {
                validationMethod.Invoke();
            }
            
            if (this is IConfirmable confirmable)
            {
                Dispatcher.UIThread.Post(confirmable.ConfirmCommand.NotifyCanExecuteChanged);
            }

            OnValidated(propertyName);
        }

        protected virtual void OnValidated(string? propertyName) {}

        protected void AddValidation(string propertyName, Action validator)
        {
            _validationMethods[propertyName] = validator;
        }

        #endregion
    }
}