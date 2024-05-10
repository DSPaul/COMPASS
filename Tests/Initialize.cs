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

            builder.RegisterType<MockNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Windowed);
            builder.RegisterType<MockNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Toast);

            App.Container = builder.Build();

            Logger.Init();
            AppDomain.CurrentDomain.FirstChanceException += Logger.LogUnhandledException;
            Logger.Debug("Logger Initialized");
        }

        [OneTimeTearDown]
        public static void MyTestCleanup() => AppDomain.CurrentDomain.FirstChanceException -= Logger.LogUnhandledException;
    }
}
