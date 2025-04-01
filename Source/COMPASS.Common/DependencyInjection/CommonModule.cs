using Autofac;
using Autofac.Features.AttributeFilters;
using COMPASS.Common.Interfaces;
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
            // Notification Services
            builder.RegisterType<WindowedNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Windowed).SingleInstance();
            builder.RegisterType<WindowedNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Toast).SingleInstance(); //use windowed for everything for now
            
            //Storage Services
            builder.RegisterType<CodexCollectionXmlStorageService>().As<ICodexCollectionStorageService>().WithAttributeFiltering();
            builder.RegisterType<ThumbnailStorageService>().As<IThumbnailStorageService>();
            builder.RegisterType<UserFilesStorageService>().As<IUserFilesStorageService>();
            
            builder.RegisterType<FilesService>().As<IFilesService>();
        }
    }
}
