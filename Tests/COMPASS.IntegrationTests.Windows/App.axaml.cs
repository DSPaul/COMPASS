using Avalonia;
using Avalonia.Markup.Xaml;


namespace COMPASS.IntegrationTests.Windows
{
    public class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);
    }
}
