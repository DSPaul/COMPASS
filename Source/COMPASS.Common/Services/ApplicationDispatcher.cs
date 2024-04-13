using Avalonia.Threading;
using System;
namespace COMPASS.Common.Services
{
    public class ApplicationDispatcher : Interfaces.IDispatcher
    {
        public void Invoke(Action method) => Dispatcher.UIThread.Invoke(method);
    }
}
