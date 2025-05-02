using Autofac;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Tools;
using Tests.Mocks;

namespace Tests
{
    [SetUpFixture]
    public class Initialize
    {
        [OneTimeSetUp]
        public void Init()
        {
            //init the container
            var builder = new ContainerBuilder();

            builder.RegisterType<MockNotificationService>().As<INotificationService>();
            builder.RegisterType<MockEnvironmentVarsService>().As<IEnvironmentVarsService>();

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
