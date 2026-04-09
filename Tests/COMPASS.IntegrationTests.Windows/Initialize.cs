using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Tests.Common.Mocks;
using Autofac;
using COMPASS.Infra.Tools;
using Logger = COMPASS.Common.Tools.Logger;
using COMPASS.Windows.DepencyInjection;

namespace COMPASS.IntegrationTests.Windows
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
            builder.RegisterModule<WindowsModule>();

            builder.RegisterType<MockNotificationService>().As<INotificationService>();
            builder.RegisterType<MockApplicationDataService>().As<IApplicationDataService>();

            ServiceResolver.Initialize(builder.Build());

            Logger.Init();
            //TODO
            //AppDomain.CurrentDomain.FirstChanceException += Logger.LogUnhandledException;
            Logger.Debug("Logger Initialized");
        }

        //TODO
        // [OneTimeTearDown]
        // public static void MyTestCleanup() => AppDomain.CurrentDomain.FirstChanceException -= Logger.LogUnhandledException;
    }
}
