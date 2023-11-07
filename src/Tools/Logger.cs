using COMPASS.Models;
using COMPASS.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.Tools
{
    public static class Logger
    {
        public static void Init() => FileLog = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        // Log To file
        public static log4net.ILog FileLog;

        // Log to Log Tab
        public static ObservableCollection<LogEntry> ActivityLog { get; private set; } = new ObservableCollection<LogEntry>();

        public static void Info(string message) =>
            Application.Current.Dispatcher.Invoke(()
                => ActivityLog.Add(new(LogEntry.MsgType.Info, message)));
        public static void Warn(string message)
        {
            Application.Current.Dispatcher.Invoke(() => ActivityLog.Add(new(LogEntry.MsgType.Warning, message)));
            FileLog.Warn(message);
        }

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
            string message = $"Its seems COMPASS has run into a critical error ({e.Exception.Message}).\n" +
                $"You can help improve COMPASS by opening an issue on {Constants.RepoURL} with the error message. \n" +
                $"Please include the log file located at {SettingsViewModel.CompassDataPath}\\logs.";
            if (Task.Run(() => MessageBox.Show(message, $"COMPASS ran into a critical error.", MessageBoxButton.OK, MessageBoxImage.Error)).Result == MessageBoxResult.OK)
            {
                e.Handled = true;
                Environment.Exit(1);
            }
        }
    }
}
