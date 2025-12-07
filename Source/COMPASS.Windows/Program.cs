using Autofac;
using Avalonia;
using Avalonia.Svg.Skia;
using COMPASS.Common;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using COMPASS.Infra.Tools;
using COMPASS.Windows.DepencyInjection;
using COMPASS.Windows.Services;

namespace COMPASS.Windows;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Create the Autofac container
        BuildContainer();

        // Set up global exception handlers
        AppDomain.CurrentDomain.UnhandledException += CrashHandler.CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += CrashHandler.TaskScheduler_UnobservedTaskException;
        
        try
        {
            CmdLineArgumentService.ParseArgs(args);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Logger.Fatal("Unhandled exception caught" ,ex);
            CrashHandler.HandleCrash(ex);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        //makes svg preview work
        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);
        ////////////////////////
        
        return AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .WithInterFont()
                         .LogToTrace();
    }

    private static void BuildContainer()
    {
        var builder = new ContainerBuilder();

        builder.RegisterModule<CommonModule>();
        builder.RegisterModule<WindowsModule>();

        ServiceResolver.Initialize(builder.Build());
    }
}
