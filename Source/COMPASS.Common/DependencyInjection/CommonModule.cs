using Autofac;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;

namespace COMPASS.Common.DependencyInjection
{
    public class CommonModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register Services
            builder.RegisterType<WindowedNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Windowed).SingleInstance();
            builder.RegisterType<WindowedNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Toast).SingleInstance(); //use windowed for everything for now
        }
    }
}
