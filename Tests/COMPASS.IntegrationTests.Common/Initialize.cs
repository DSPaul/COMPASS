using Autofac;
using COMPASS.Common.DependencyInjection;
using COMPASS.Infra.Tools;
using COMPASS.Tests.Common;

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
            builder.RegisterModule<MockModule>();

            ServiceResolver.Initialize(builder.Build());

            //TODO
            //AppDomain.CurrentDomain.FirstChanceException += Logger.LogUnhandledException;
        }

        //TODO
        // [OneTimeTearDown]
        // public static void MyTestCleanup() => AppDomain.CurrentDomain.FirstChanceException -= Logger.LogUnhandledException;
    }
}
