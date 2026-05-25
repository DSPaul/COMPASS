using Autofac;
using COMPASS.Common.DependencyInjection;
using COMPASS.Windows.DepencyInjection;

namespace COMPASS.IntegrationTests.Windows
{
    [TestFixture]
    public class DepencyInjection
    {
        [Test]
        public void Container_ShouldResolveAllRegistrations_WithoutCircularDependencies()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<CommonModule>();
            builder.RegisterModule<WindowsModule>();
            var container = builder.Build();

            foreach (var registration in container.ComponentRegistry.Registrations)
            {
                foreach (var service in registration.Services)
                {
                    container.ResolveService(service); // will throw on any cycle
                }
            }
        }
    }
}
