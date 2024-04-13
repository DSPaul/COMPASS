using System;

namespace COMPASS.Common.Interfaces
{
    public interface IDispatcher
    {
        void Invoke(Action method);
    }
}
