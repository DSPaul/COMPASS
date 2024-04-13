using COMPASS.Interfaces;

namespace Tests.Mocks
{
    internal class MockDispatcher : IDispatcher
    {
        public void Invoke(Action method) => method.DynamicInvoke();
    }
}
