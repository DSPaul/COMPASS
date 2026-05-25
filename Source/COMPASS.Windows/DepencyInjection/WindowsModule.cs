using Autofac;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Windows.Services;

namespace COMPASS.Windows.DepencyInjection
{
    public class WindowsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<IOService>().As<IIOService>();
            builder.RegisterType<UIService>().As<IUIService>();
        }
    }
}
