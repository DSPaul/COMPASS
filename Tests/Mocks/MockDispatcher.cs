using COMPASS.Interfaces;

namespace Tests.Mocks
{
    internal class MockDispatcher : IDispatcher
    {
        public void Dispatch(Delegate method, params object[] args) => method.DynamicInvoke(args);

        public void Invoke(Delegate method) => method.DynamicInvoke();
    }
}
