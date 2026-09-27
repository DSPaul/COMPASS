using Autofac;
using COMPASS.Common.DependencyInjection;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.IO;
using COMPASS.Infra.Notifications;
using COMPASS.Linux.DependencyInjection;
using COMPASS.Tests.Common.Mocks;

namespace COMPASS.IntegrationTests.Linux
{
    [SetUpFixture]
    public class Initialize
    {
        [OneTimeSetUp]
        public void Init()
        {
            //init the container
            var builder = new ContainerBuilder();

            builder.RegisterModule<CommonModule>();
            builder.RegisterModule<LinuxModule>();

            builder.RegisterType<MockNotificationService>().As<INotificationService>();
            builder.RegisterType<MockApplicationDataService>().As<IApplicationDataService>();

            ServiceResolver.Initialize(builder.Build());

            //TODO
            //AppDomain.CurrentDomain.FirstChanceException += Logger.LogUnhandledException;
        }

        //TODO
        // [OneTimeTearDown]
        // public static void MyTestCleanup() => AppDomain.CurrentDomain.FirstChanceException -= Logger.LogUnhandledException;
    }
}
