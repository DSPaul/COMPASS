using Autofac;
using COMPASS.Common.Interfaces.Repos;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Repositories;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Services.Storage;

namespace COMPASS.Common.DependencyInjection
{
    public class CommonModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Notification Service
            builder.RegisterType<NotificationService>().As<INotificationService>();

            //Data releted services and repositories
            builder.RegisterType<CodexCollectionXmlRepository>().Keyed<ICodexCollectionRepository>(StorageStrategy.Xml);
            builder.RegisterType<CodexCollectionMemRepository>().Keyed<ICodexCollectionRepository>(StorageStrategy.Memory);
            builder.RegisterType<ImportExportService>().As<IImportExportService>();
            builder.RegisterType<CoverStorageService>().As<ICoverStorageService>();
            builder.RegisterType<UserFilesStorageService>().As<IUserFilesStorageService>();
            
            builder.RegisterType<ApplicationDataService>()
                .As<IApplicationDataService>()
                .SingleInstance();

            builder.RegisterType<FilesService>().As<IFilesService>();
            builder.RegisterType<WebService>().As<IWebService>();
        }
    }
}
