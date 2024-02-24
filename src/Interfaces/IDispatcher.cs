using System;

namespace COMPASS.Interfaces
{
    public interface IDispatcher
    {
        void Invoke(Delegate method);
    }
}
