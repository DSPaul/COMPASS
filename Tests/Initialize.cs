using Autofac;
using COMPASS;
using COMPASS.Interfaces;
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
            builder.RegisterType<MockMessageBox>().As<IMessageBox>();

            App.Container = builder.Build();

            Logger.Init();
            AppDomain.CurrentDomain.FirstChanceException += Logger.LogUnhandledException;
            Logger.Debug("Logger Initialized");
        }

        [AssemblyCleanup]
        public static void MyTestCleanup() => AppDomain.CurrentDomain.FirstChanceException -= Logger.LogUnhandledException;
    }
}
