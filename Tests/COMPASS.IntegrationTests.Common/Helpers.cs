using Autofac;
using COMPASS.Common.DependencyInjection;
using COMPASS.Tests.Common;

namespace COMPASS.IntegrationTests.Common
{
    public static class Helpers
    {
        public static IContainer SetupContainer(Action<ContainerBuilder> configure)
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule<CommonModule>();
            builder.RegisterModule<MockModule>();

            configure(builder);

            return builder.Build();
        }
    }
}
