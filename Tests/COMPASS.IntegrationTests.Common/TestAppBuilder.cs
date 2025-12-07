using Avalonia;
using Avalonia.Headless;
using COMPASS.IntegrationTests.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]
namespace COMPASS.IntegrationTests.Common;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}