using Autofac;
using Autofac.Extensions.DependencyInjection;
using COMPASS.ApiClients.Compass;
using COMPASS.ApiClients.GitHub;
using Microsoft.Extensions.DependencyInjection;

namespace COMPASS.ApiClients
{
    public class ApiClientsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var services = new ServiceCollection();

            //Add http clients
            services.AddHttpClient();
            services.AddHttpClient(ICompassApiClient.HttpClientName, client =>
            {
                client.DefaultRequestHeaders.Add("Api-Key", CompassApiClient.ApiKey);
            });
            services.AddHttpClient(IGitHubApiClient.HttpClientName, client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("COMPASS");
            });
            builder.Populate(services);

            //Add api clients
            builder.RegisterType<CompassApiClient>().As<ICompassApiClient>();
            builder.RegisterType<GitHubApiClient>().As<IGitHubApiClient>();
        }
    }
}
