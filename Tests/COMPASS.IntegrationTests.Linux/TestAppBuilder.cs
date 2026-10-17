using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(COMPASS.IntegrationTests.Linux.TestAppBuilder))]

namespace COMPASS.IntegrationTests.Linux;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}