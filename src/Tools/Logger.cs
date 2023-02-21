using COMPASS.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.Tools
{
    public static class Logger
    {
        // Log To file
        public static readonly log4net.ILog FileLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Log to Log Tab
        public static ObservableCollection<LogEntry> ActivityLog { get; private set; } = new ObservableCollection<LogEntry>();

        public static void Info(string message) =>
            Application.Current.Dispatcher.Invoke(()
                => ActivityLog.Add(new(LogEntry.MsgType.Info, message)));
        public static void Warn(string message, Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => ActivityLog.Add(new(LogEntry.MsgType.Warning, message)));
            FileLog.Warn(message, ex);
        }

        public static void Error(string message, Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => ActivityLog.Add(new(LogEntry.MsgType.Error, message)));
            FileLog.Error(message, ex);
        }

        public static void LogUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            FileLog.Fatal(e.Exception.ToString(), e.Exception);
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
