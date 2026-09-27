using Autofac;
using COMPASS.Infra.Avalonia.Application;
using COMPASS.Infra.IO;
using COMPASS.Infra.Updates;
using COMPASS.Windows.Services;

namespace COMPASS.Windows.DependencyInjection
{
    public class WindowsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<IOService>().As<IIOService>();
            builder.RegisterType<UIService>().As<IUIService>();
            builder.RegisterType<UpdateService>().As<IUpdateService>();
        }
    }
}
