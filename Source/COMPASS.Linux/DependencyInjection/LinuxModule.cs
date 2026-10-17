using Autofac;
using COMPASS.Infra.Avalonia.Application;
using COMPASS.Infra.IO;
using COMPASS.Infra.Updates;
using COMPASS.Linux.Services;

namespace COMPASS.Linux.DependencyInjection
{
    public class LinuxModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<IOService>().As<IIOService>();
            builder.RegisterType<UIService>().As<IUIService>();
            builder.RegisterType<UpdateService>().As<IUpdateService>();
        }
    }
}
