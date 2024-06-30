using Autofac;
using COMPASS;
using COMPASS.Interfaces;
using COMPASS.Models.Enums;
using COMPASS.Tools;
using Tests.Mocks;

namespace Tests
{
    [TestClass]
    public static class Initialize
    {
        [AssemblyInitialize]
        public static void MyTestInitialize(TestContext testContext)
        {
            //init the container
            var builder = new ContainerBuilder();

            builder.RegisterType<MockDispatcher>().As<IDispatcher>();
            builder.RegisterType<MockNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Windowed);
            builder.RegisterType<MockNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Toast);

            App.Container = builder.Build();

            Logger.Init();
            AppDomain.CurrentDomain.FirstChanceException += Logger.LogUnhandledException;
            Logger.Debug("Logger Initialized");
        }

        [AssemblyCleanup]
        public static void MyTestCleanup() => AppDomain.CurrentDomain.FirstChanceException -= Logger.LogUnhandledException;
    }
}
