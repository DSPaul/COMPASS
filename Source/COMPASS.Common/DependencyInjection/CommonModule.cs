using Autofac;
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
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Services.Storage;
using COMPASS.Common.Sources;
using COMPASS.Common.Tools.Logging;
using COMPASS.Common.ViewModels.Layouts;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Avalonia.DependencyInjection;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Notifications;
using COMPASS.Infra.Preferences;
using COMPASS.Infra.Web;

namespace COMPASS.Common.DependencyInjection
{
    public class CommonModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Modules
            builder.RegisterModule<ApiClientsModule>();
            builder.RegisterModule<InfraModule>();
            builder.RegisterModule<AvaloniaModule>();

            //Http clients
            RegisterHttpClients(builder);

            // Logging: single Serilog pipeline (rolling file + in-app panel sinks),
            // decorated so logs inside a LoggerScope also reach the scoped tracker.
            builder.RegisterType<SerilogLogger>().As<ILogger>().SingleInstance();

            //Data Repos: singletons so locks on files work
            builder.RegisterType<CodexCollectionXmlRepository>().Keyed<ICodexCollectionRepository>(StorageStrategy.Xml).SingleInstance();
            builder.RegisterType<CodexCollectionMemRepository>().Keyed<ICodexCollectionRepository>(StorageStrategy.Memory).SingleInstance();

            RegisterMetadataSources(builder);
            RegisterLayouts(builder);

            //Services with cache of some kind -> SingleInstance
            builder.RegisterType<PreferencesService>().As<IPreferencesService>().SingleInstance();

            //Managers (stateful services)
            builder.RegisterType<CollectionManager>().AsSelf().SingleInstance();

            //Services are singletons: all are stateless and several are held by singleton
            //operations/managers where transient would just pin one copy per consumer anyway
            builder.RegisterType<ApplicationDataService>().As<IApplicationDataService>().SingleInstance();
            builder.RegisterType<CoverService>().As<ICoverService>().SingleInstance();
            builder.RegisterType<CoverStorageService>().As<ICoverStorageService>().SingleInstance();
            builder.RegisterType<FilterService>().As<IFilterService>().SingleInstance();
            builder.RegisterType<ImportExportService>().As<IImportExportService>().SingleInstance();
            builder.RegisterType<NotificationService>().As<INotificationService>().SingleInstance();
            builder.RegisterType<UserFilesStorageService>().As<IUserFilesStorageService>().SingleInstance();

            //Operations
            builder.RegisterType<Operations.CodexCollectionOperations>().AsSelf().SingleInstance();
            builder.RegisterType<Operations.TagOperations>().AsSelf().SingleInstance();
            builder.RegisterType<Operations.CodexOperations>().AsSelf().SingleInstance();

            // View model factories (all classes marked with [Factory])
            builder.RegisterFactories(typeof(CommonModule).Assembly);
            builder.RegisterFactories(typeof(InfraModule).Assembly);
            builder.RegisterFactories(typeof(AvaloniaModule).Assembly);

            // ViewModel singletons
            builder.RegisterType<TabsViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<MainViewModel>().AsSelf().SingleInstance();
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
            Dictionary<MetadataSourceType, Type> metaDataSources = new()
            {
                { MetadataSourceType.File, typeof(FileMetadataSource) },
                { MetadataSourceType.PDF, typeof(PdfMetadataSource) },
                { MetadataSourceType.Image, typeof(ImageMetadataSource) },
                { MetadataSourceType.ISBN, typeof(ISBNMetadataSource) },
                { MetadataSourceType.GmBinder, typeof(GmBinderMetadataSource) },
                { MetadataSourceType.Homebrewery, typeof(HomebreweryMetadataSource) },
                { MetadataSourceType.GoogleDrive, typeof(GoogleDriveMetadataSource) },
                { MetadataSourceType.GenericURL, typeof(GenericOnlineMetadataSource) }
            };

            foreach (var kvp in metaDataSources)
            {
                builder.RegisterType(kvp.Value).Keyed<MetadataSource>(kvp.Key.ToString()).SingleInstance();
            }
        }

        private static void RegisterHttpClients(ContainerBuilder builder)
        {
            builder.RegisterHttpClient(ICompassApiClient.HttpClientName);
            builder.RegisterHttpClient(IGitHubApiClient.HttpClientName);
        }
    }
}
