namespace COMPASS.Infra.Objects;

public interface IItemWrapper<T>
{
    T Item { get; set; }
}