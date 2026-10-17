namespace COMPASS.Infra.Objects;

public interface ICloneable<T> where T : ICloneable<T>
{
    void CopyFrom(T source);

    T Clone();
}