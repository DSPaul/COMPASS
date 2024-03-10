using COMPASS.Interfaces;

namespace Tests.Mocks
{
    internal class MockDispatcher : IDispatcher
    {
        public void Invoke(Delegate method) => method.DynamicInvoke();
        public void Invoke(Delegate method, params object[] args) => method.DynamicInvoke(args);
    }
}
