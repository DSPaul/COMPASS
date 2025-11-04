using System.Collections.ObjectModel;

namespace COMPASS.Infra.Models.Interfaces;

public interface IHasChildren<T> where T : IHasChildren<T>
{
    ObservableCollection<T> Children { get; }
}