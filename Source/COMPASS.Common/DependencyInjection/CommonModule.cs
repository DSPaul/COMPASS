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
using COMPASS.Common.Sources;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Services.Storage;
using COMPASS.Common.Tools.Logging;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Layouts;
using COMPASS.Common.ViewModels.Main;
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
            RegisterHttpClients(builder);

            // Logging: single Serilog pipeline (rolling file + in-app panel sinks)
            builder.RegisterType<SerilogLogger>().As<ILogger>().SingleInstance();

            //Data Repos: singletons so locks on files work
            builder.RegisterType<CodexCollectionXmlRepository>().Keyed<ICodexCollectionRepository>(StorageStrategy.Xml).SingleInstance();
            builder.RegisterType<CodexCollectionMemRepository>().Keyed<ICodexCollectionRepository>(StorageStrategy.Memory).SingleInstance();

            RegisterMetadataSources(builder);
            RegisterLayouts(builder);

            //Services with cache of some kind -> SingleInstance
            builder.RegisterType<PreferencesService>().As<IPreferencesService>().SingleInstance();
            builder.RegisterType<WebDriverService>().As<IWebDriverService>().SingleInstance();

            //Managers (stateful services)
            builder.RegisterType<UpdateManager>().AsSelf().SingleInstance();
            builder.RegisterType<CollectionManager>().AsSelf().SingleInstance();
            builder.RegisterType<ConnectivityManager>().AsSelf().SingleInstance();

            //Services are singletons: all are stateless and several are held by singleton
            //operations/managers where transient would just pin one copy per consumer anyway
            builder.RegisterType<ApplicationDataService>().As<IApplicationDataService>().SingleInstance();
            builder.RegisterType<BarcodeDecoderService>().As<IBarcodeDecoderService>().SingleInstance();
            builder.RegisterType<CameraService>().As<ICameraService>().SingleInstance();
            builder.RegisterType<CoverService>().As<ICoverService>().SingleInstance();
            builder.RegisterType<CoverStorageService>().As<ICoverStorageService>().SingleInstance();
            builder.RegisterType<FilesService>().As<IFilesService>().SingleInstance();
            builder.RegisterType<FilterService>().As<IFilterService>().SingleInstance();
            builder.RegisterType<ImportExportService>().As<IImportExportService>().SingleInstance();
            builder.RegisterType<NotificationService>().As<INotificationService>().SingleInstance();
            builder.RegisterType<UserFilesStorageService>().As<IUserFilesStorageService>().SingleInstance();
            builder.RegisterType<WebService>().As<IWebService>().SingleInstance();

            //Operations
            builder.RegisterType<Operations.CodexCollectionOperations>().AsSelf().SingleInstance();
            builder.RegisterType<Operations.TagOperations>().AsSelf().SingleInstance();
            builder.RegisterType<Operations.CodexOperations>().AsSelf().SingleInstance();

            // View model factories (all classes marked with [Factory])
            builder.RegisterFactories();

            // ViewModel singletons
            builder.RegisterType<TabsViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<MainViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<ProgressViewModel>().AsSelf().SingleInstance();
        }

        private static void RegisterLayouts(ContainerBuilder builder)
        {
            Dictionary<CodexLayout, Type> layouts = new()
            {
                { CodexLayout.Home, typeof(HomeLayoutViewModelFactory) },
                { CodexLayout.List, typeof(ListLayoutViewModelFactory) },
                { CodexLayout.Card, typeof(CardLayoutViewModelFactory) },
                { CodexLayout.Tile, typeof(TileLayoutViewModelFactory) }
            };

            foreach (var kvp in layouts)
            {
                builder.RegisterType(kvp.Value).Keyed<LayoutViewModelFactoryBase>(kvp.Key.ToString());
            }
        }

        private static void RegisterMetadataSources(ContainerBuilder builder)
        {
            Dictionary<MetaDataSourceType, Type> metaDataSources = new()
            {
                { MetaDataSourceType.File, typeof(FileMetaDataSource) },
                { MetaDataSourceType.PDF, typeof(PdfMetaDataSource) },
                { MetaDataSourceType.Image, typeof(ImageMetaDataSource) },
                { MetaDataSourceType.ISBN, typeof(ISBNMetaDataSource) },
                { MetaDataSourceType.GmBinder, typeof(GmBinderMetaDataSource) },
                { MetaDataSourceType.Homebrewery, typeof(HomebreweryMetaDataSource) },
                { MetaDataSourceType.GoogleDrive, typeof(GoogleDriveMetaDataSource) },
                { MetaDataSourceType.GenericURL, typeof(GenericOnlineMetaDataSource) }
            };

            foreach (var kvp in metaDataSources)
            {
                builder.RegisterType(kvp.Value).Keyed<MetaDataSource>(kvp.Key.ToString()).SingleInstance();
            }
        }

        private static void RegisterHttpClients(ContainerBuilder builder)
        {
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
        }
    }
}
