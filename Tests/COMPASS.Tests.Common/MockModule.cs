using Autofac;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Services;
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
            builder.RegisterType<MockLogger>().As<ILogger>().AsSelf().SingleInstance();
            builder.RegisterType<MockNotificationService>().As<INotificationService>();
            builder.RegisterType<MockPreferencesService>().As<IPreferencesService>();
        }
    }
}
