using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using COMPASS.Models;
using System.IO;

namespace COMPASS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            //Create necessary Romaing Folders
            Directory.CreateDirectory((Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\DnD\CoverArt"));
            Directory.CreateDirectory((Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\PathFinder\CoverArt"));
        }
    }

}
