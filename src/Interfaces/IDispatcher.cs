using System;

namespace COMPASS.Interfaces
{
    public interface IDispatcher
    {
        void Invoke(Delegate method);
        void Invoke(Delegate method, object[] args);

        void BeginInvoke(Delegate method);
        void beginInvoke(Delegate method, object[] args);
    }
}
