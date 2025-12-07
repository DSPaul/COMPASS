using Avalonia;
using Avalonia.Markup.Xaml;


namespace COMPASS.IntegrationTests.Linux;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}