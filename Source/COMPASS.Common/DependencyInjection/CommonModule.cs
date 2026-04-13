using Autofac;
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

namespace COMPASS.Common.DependencyInjection
{
    public class CommonModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
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

            builder.RegisterType<ApplicationDataService>().As<IApplicationDataService>().SingleInstance();

            // Misc Services
            builder.RegisterType<NotificationService>().As<INotificationService>();
            builder.RegisterType<PrereleaseUpdateService>().As<IUpdateService>();
            builder.RegisterType<FilesService>().As<IFilesService>();
            builder.RegisterType<WebService>().As<IWebService>();
        }
    }
}
