using Autofac;
using COMPASS.Infra.Application;
using COMPASS.Infra.Avalonia.Application;
using COMPASS.Infra.Avalonia.Barcode;
using COMPASS.Infra.Avalonia.Camera;
using COMPASS.Infra.Avalonia.Files;

namespace COMPASS.Infra.Avalonia.DependencyInjection
{
    public class AvaloniaModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ApplicationService>().As<IApplicationService>().SingleInstance();
            builder.RegisterType<BarcodeDecoderService>().As<IBarcodeDecoderService>().SingleInstance();
            builder.RegisterType<CameraService>().As<ICameraService>().SingleInstance();
            builder.RegisterType<FilesService>().As<IFilesService>().SingleInstance();
        }
    }
}
