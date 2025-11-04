namespace COMPASS.Infra.Models.Interfaces;

public interface IItemWrapper<T>
{
    T Item { get; set; }
}