using Autofac;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using COMPASS.Common.DependencyInjection;
using COMPASS.Infra.Avalonia.Modal;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Preferences;

namespace COMPASS.Tests.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //Minimal container so view models with static accessors or factory dependencies can resolve
            var builder = new ContainerBuilder();
            builder.RegisterModule<CommonModule>();
            builder.RegisterType<SilentLogger>().As<ILogger>();
            ServiceResolver.Initialize(builder.Build());

            desktop.MainWindow = WindowManager.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private class SilentLogger : ILogger
    {
        public void Info(string message) { }
        public void Debug(string message, Exception? ex = null) { }
        public void Warn(string message, Exception? ex = null) { }
        public void Error(string message, Exception ex) { }
        public void Fatal(string message, Exception ex) { }
    }

    private class InMemoryPreferencesService : IPreferencesService
    {
        public T GetPreferences<T>() where T : IPreferences, new() => new T();
        public void SavePreferences() { }
    }
}
