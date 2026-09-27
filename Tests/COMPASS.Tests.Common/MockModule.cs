using Autofac;
using COMPASS.Infra.Avalonia.Files;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Notifications;
using COMPASS.Infra.Preferences;
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
