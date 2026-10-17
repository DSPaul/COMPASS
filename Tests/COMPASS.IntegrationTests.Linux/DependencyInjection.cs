using Autofac;
using COMPASS.Common.DependencyInjection;
using COMPASS.Linux.DependencyInjection;

namespace COMPASS.IntegrationTests.Linux
{
    [TestFixture]
    public class DependencyInjection
    {
        [Test]
        public void Container_ShouldResolveAllRegistrations_WithoutCircularDependencies()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<CommonModule>();
            builder.RegisterModule<LinuxModule>();
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
