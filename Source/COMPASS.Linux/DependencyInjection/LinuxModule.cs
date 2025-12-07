using Autofac;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Linux.Services;

namespace COMPASS.Linux.DepencyInjection
{
    public class LinuxModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<EnvironmentVarsService>().As<IEnvironmentVarsService>();
            builder.RegisterType<IOService>().As<IIOService>();
            builder.RegisterType<UIService>().As<IUIService>();
            builder.RegisterType<WebDriverService>().As<IWebDriverService>().InstancePerLifetimeScope();
        }
    }
}
