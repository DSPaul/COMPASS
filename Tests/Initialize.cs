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
            Logger.Warn("Logger Initialized");
        }
    }
}
