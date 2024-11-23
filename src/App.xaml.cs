using Autofac;
using COMPASS.Interfaces;
using COMPASS.Models.Enums;
using COMPASS.Services;
using COMPASS.ViewModels;
using System;
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
            try
            {
                if (!Directory.Exists(SettingsViewModel.CompassDataPath))
                {
                    Directory.CreateDirectory(SettingsViewModel.CompassDataPath);
                }
            }
            catch (Exception ex)
            {
                //Cannot show notification here because app needs to finish its constructor before any UI can be shown,
                //Cannot log either because the log file is located in the CompassDataPath directory
            }
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
