using Avalonia.Threading;
using System;
namespace COMPASS.Services
{
    public class ApplicationDispatcher : Interfaces.IDispatcher
    {
        public void Invoke(Action method) => Dispatcher.UIThread.Invoke(method);
    }
}
