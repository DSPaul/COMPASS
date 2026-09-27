namespace COMPASS.Infra.Collections;

public interface IHasChildren<T> where T : IHasChildren<T>
{
    RangeObservableCollection<T> Children { get; }
}