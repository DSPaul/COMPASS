using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Common.ViewModels.ModelVMs;

/// <summary>
/// A base class for viewmodels that map 1 to 1 to a model
/// </summary>
/// <typeparam name="TModel"></typeparam>
public abstract class ModelViewModelBase<TModel> : ViewModelBase, IDisposable 
    where TModel : ObservableObject
{
    protected readonly TModel _model;
    protected readonly Dictionary<string, IList<string>> _derivedProperties = [];
    
    public ModelViewModelBase(TModel model)
    {
        _model = model;
        model.PropertyChanged += OnModelPropertyChanged;
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == null) return;
        HandlePropertyChanged(e.PropertyName);
    }

    protected void HandlePropertyChanged(string propertyName)
    {
        //Notify property changed on prop itself
        Dispatcher.UIThread.Invoke(() => OnPropertyChanged(propertyName));
        
        //Notify all derived Properties
        if (_derivedProperties.TryGetValue(propertyName, out var derivedPropertiesList))
        {
            foreach (var derivedProperty in derivedPropertiesList)
            {
                Dispatcher.UIThread.Invoke(() => OnPropertyChanged(derivedProperty));
            }
        }
        
        //Validate prop
        Validate(propertyName);
    }

    public TModel GetModel() => _model;
    
    public virtual void Dispose()
    {
        _model.PropertyChanged -= OnModelPropertyChanged;
    }
}