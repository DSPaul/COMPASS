using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace COMPASS.Infra.Web
{
    public static class WebExtensions
    {
        extension(ContainerBuilder builder)
        {
            public void RegisterHttpClient(string serviceName)
            {
                var services = new ServiceCollection();
                services.AddHttpClient(serviceName).AddHttpMessageHandler<ConnectivityHandler>();
                builder.Populate(services);
            }

            public void RegisterHttpClient(string serviceName, Action<HttpClient> configure)
            {
                var services = new ServiceCollection();
                services.AddHttpClient(serviceName, configure).AddHttpMessageHandler<ConnectivityHandler>();
                builder.Populate(services);
            }
        }
    }
}
