using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(COMPASS.IntegrationTests.Windows.TestAppBuilder))]

namespace COMPASS.IntegrationTests.Windows;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}