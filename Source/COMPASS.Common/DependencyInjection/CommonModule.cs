using Autofac;
using Autofac.Extensions.DependencyInjection;
using COMPASS.ApiClients;
using COMPASS.ApiClients.Compass;
using COMPASS.ApiClients.GitHub;
using COMPASS.Common.Interfaces.Repos;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Repositories;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Services.Storage;
using COMPASS.Common.Tools.Logging;
using COMPASS.Infra.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace COMPASS.Common.DependencyInjection
{
    public class CommonModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // API Clients
            builder.RegisterModule<ApiClientsModule>();

            // HttpClients for common services
            var services = new ServiceCollection();
            services.AddTransient<ConnectivityHandler>();
            services.AddHttpClient(WebService.BrowserHttpClient, client =>
            {
                //Add user agents to mimic browser, some servers block requests without user agents or with non-browser user agents
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36");
            });
            services.AddHttpClient(WebService.ConnectionCheckHttpClient, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(3);
            });
            
            // Attach the connectivity handler to API clients so they also update IsOnline
            services.AddHttpClient(ICompassApiClient.HttpClientName)
                    .AddHttpMessageHandler<ConnectivityHandler>();
            services.AddHttpClient(IGitHubApiClient.HttpClientName)
                    .AddHttpMessageHandler<ConnectivityHandler>();
            builder.Populate(services);
            
            // Logging
            builder.RegisterType<FileLogger>().AsSelf().SingleInstance();
            builder.RegisterType<UILogger>().AsSelf().SingleInstance();
            builder.Register(c => new CompositeLogger([c.Resolve<FileLogger>(), c.Resolve<UILogger>()]))
                   .As<ILogger>()
                   .SingleInstance();

            //Data releted services and repositories
            builder.RegisterType<CodexCollectionXmlRepository>().Keyed<ICodexCollectionRepository>(StorageStrategy.Xml);
            builder.RegisterType<CodexCollectionMemRepository>().Keyed<ICodexCollectionRepository>(StorageStrategy.Memory);
            builder.RegisterType<ImportExportService>().As<IImportExportService>();
            builder.RegisterType<CoverStorageService>().As<ICoverStorageService>();
            builder.RegisterType<UserFilesStorageService>().As<IUserFilesStorageService>();

            ///Singletons
            builder.RegisterType<ApplicationDataService>().As<IApplicationDataService>().SingleInstance();
            builder.RegisterType<PreferencesService>().As<IPreferencesService>().SingleInstance();
            builder.RegisterType<WebDriverService>().As<IWebDriverService>().SingleInstance();

            // Misc Services
            builder.RegisterType<NotificationService>().As<INotificationService>();
            builder.RegisterType<PrereleaseUpdateService>().As<IUpdateService>();
            builder.RegisterType<FilesService>().As<IFilesService>();
            builder.RegisterType<WebService>().As<IWebService>();
        }
    }
}
