using Autofac;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Tests.Common.Mocks;

namespace COMPASS.Tests.Common
{
    public class MockModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MockApplicationDataService>().As<IApplicationDataService>();
            builder.RegisterType<MockFilesService>().As<IFilesService>();
            builder.RegisterType<MockIOService>().As<IIOService>();
            builder.RegisterType<MockLogger>().As<ILogger>();
            builder.RegisterType<MockNotificationService>().As<INotificationService>();
            builder.RegisterType<MockWebDriverService>().As<IWebDriverService>();
        }
    }
}
