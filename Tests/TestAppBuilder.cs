using Avalonia;
using Avalonia.Headless;


[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<Tests.App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
