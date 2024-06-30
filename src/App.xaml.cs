using Autofac;
using COMPASS.Interfaces;
using COMPASS.Models.Enums;
using COMPASS.Services;
using COMPASS.ViewModels;
using System.IO;
using System.Windows;

namespace COMPASS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Directory.CreateDirectory(SettingsViewModel.CompassDataPath);
        }

        private static IContainer? _container;

        public static IContainer Container
        {
            get
            {
                if (_container is null)
                {
                    //init the container
                    var builder = new ContainerBuilder();

                    builder.RegisterType<ApplicationDispatcher>().As<IDispatcher>();
                    builder.RegisterType<WindowedNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Windowed);
                    builder.RegisterType<WindowedNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Toast); //use windowed for everything for now

                    _container = builder.Build();
                }
                return _container;
            }
            set => _container = value;
        }

        private static IDispatcher? _dispatcher;
        public static IDispatcher SafeDispatcher => _dispatcher ??= Container.Resolve<IDispatcher>();
    }
}
