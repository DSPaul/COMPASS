using Autofac;
using COMPASS.Interfaces;
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
                    builder.RegisterType<MessageDialog>().As<IMessageBox>();

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
