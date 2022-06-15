using COMPASS.Tools;
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
            if (!Directory.Exists(SettingsViewModel.CompassDataPath))
            {
                Directory.CreateDirectory(SettingsViewModel.CompassDataPath);
            }
        }
    }

}
