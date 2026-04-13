using Autofac;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Infra.Tools;
using COMPASS.Tests.Common.Mocks;

namespace COMPASS.IntegrationTests.Common
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

            builder.RegisterType<MockNotificationService>().As<INotificationService>();
            builder.RegisterType<MockApplicationDataService>().As<IApplicationDataService>();
            builder.RegisterType<MockWebDriverService>().As<IWebDriverService>();
            builder.RegisterType<MockIOService>().As<IIOService>();

            ServiceResolver.Initialize(builder.Build());

            //TODO
            //AppDomain.CurrentDomain.FirstChanceException += Logger.LogUnhandledException;
        }

        //TODO
        // [OneTimeTearDown]
        // public static void MyTestCleanup() => AppDomain.CurrentDomain.FirstChanceException -= Logger.LogUnhandledException;
    }
}
