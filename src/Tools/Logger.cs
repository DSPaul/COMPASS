using COMPASS.Models;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.Tools
{
    public static class Logger
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void LogUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = e.Exception.InnerException.ToString();
            log.Fatal(errorMessage);
            //prompt user to submit logs and open an issue
            string message = "Its seems COMPASS has run into an error and crashed.\n" +
                $"You can help improve COMPASS by opening an issue on {Constants.RepoURL}. \n" +
                @"Please include the log file located at %appdata%\COMPASS\logs.";
            if (Task.Run(() => MessageBox.Show(message, "COMPASS has crashed.", MessageBoxButton.OK, MessageBoxImage.Error)).Result == MessageBoxResult.OK)
            {
                e.Handled = true;
                Environment.Exit(1);
            }
        }
    }
}
