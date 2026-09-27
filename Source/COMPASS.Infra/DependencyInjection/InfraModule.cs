using Autofac;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Progress;
using COMPASS.Infra.Updates;
using COMPASS.Infra.Web;

namespace COMPASS.Infra.DependencyInjection
{
    public class InfraModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //Logging
            builder.RegisterDecorator<ScopedForwardingLogger, ILogger>();

            //Progress
            builder.RegisterType<ProgressTrackingManager>().AsSelf().SingleInstance();

            //Updates
            builder.RegisterType<UpdateManager>().AsSelf().SingleInstance();

            //Web
            builder.RegisterType<ConnectivityHandler>().AsSelf();
            builder.RegisterType<ConnectivityManager>().AsSelf().SingleInstance();
            builder.RegisterType<WebDriverService>().As<IWebDriverService>().SingleInstance();
            builder.RegisterType<WebService>().As<IWebService>().SingleInstance();

            builder.RegisterHttpClient(WebService.BrowserHttpClient, static client =>
            {
                //Add user agents to mimic browser, some servers block requests without user agents or with non-browser user agents
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36");
            });

            builder.RegisterHttpClient(WebService.ConnectionCheckHttpClient, static client =>
            {
                client.Timeout = TimeSpan.FromSeconds(3);
            });
        }
    }
}
