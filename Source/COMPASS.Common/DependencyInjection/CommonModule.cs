using Autofac;
using Autofac.Features.AttributeFilters;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models.Enums;
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
            
            //Storage Services
            builder.RegisterType<CodexCollectionXmlStorageService>().As<ICodexCollectionStorageService>();
            builder.RegisterType<ThumbnailStorageService>().As<IThumbnailStorageService>();
            builder.RegisterType<UserFilesStorageService>().As<IUserFilesStorageService>();
            
            builder.RegisterType<FilesService>().As<IFilesService>();
            builder.RegisterType<ApplicationDataService>().As<IApplicationDataService>();
        }
    }
}
