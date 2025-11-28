using System;
using Avalonia.Media;
using COMPASS.Common.Models.Filters;

namespace COMPASS.Common.ViewModels.ModelVMs;

public class FilterViewModel : IEquatable<FilterViewModel> //Don't inherit from ModelViewModelBase because filters only have readonly props
{
    public FilterViewModel(Filter model, Color backgroundColor, string content)
    {
        _model = model;
        
        BackgroundColor = backgroundColor;
        Content = content;
    }

    private readonly Filter _model;

    public Filter GetModel() => _model;
    
    public Color BackgroundColor { get; }

    public string Content { get; }
    
    public FilterType Type => _model.Type;
    
    
    #region IEquatable
    public override bool Equals(object? obj) => Equals(obj as FilterViewModel);

    public bool Equals(FilterViewModel? other)
    {
        if (other == null) return false;
        return _model.Equals(other._model);  
    }
    
    public static bool operator ==(FilterViewModel? lhs, FilterViewModel? rhs)
    {
        if (lhs is null)
        {
            return rhs is null; //if lhs is null, only equal if rhs is also null
        }
        // Equals handles case of null on right side.
        return lhs.Equals(rhs);
    }
    public static bool operator !=(FilterViewModel? lhs, FilterViewModel? rhs)
    {
        return !(lhs == rhs);
    }

    public override int GetHashCode() => _model.GetHashCode();
    #endregion
}