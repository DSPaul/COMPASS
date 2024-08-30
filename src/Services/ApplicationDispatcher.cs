using COMPASS.Interfaces;
using System;
using System.Windows.Threading;

namespace COMPASS.Services
{
    public class ApplicationDispatcher : IDispatcher
    {
        public void Invoke(Delegate method) => UnderlyingDispatcher.Invoke(method);
        public void Invoke(Delegate method, object[] args) => UnderlyingDispatcher.Invoke(method, args);

        private static Dispatcher UnderlyingDispatcher
        {
            get
            {
                if (System.Windows.Application.Current == null)
                    throw new InvalidOperationException("You must call this method from within a running WPF application!");

                if (System.Windows.Application.Current.Dispatcher == null)
                    throw new InvalidOperationException("You must call this method from within a running WPF application with an active dispatcher!");

                return System.Windows.Application.Current.Dispatcher;
            }
        }
    }
}
