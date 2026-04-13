using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Tests.Common.Mocks;
using Autofac;
using COMPASS.Infra.Tools;
using COMPASS.Linux.DepencyInjection;

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
