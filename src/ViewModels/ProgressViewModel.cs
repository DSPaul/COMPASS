using COMPASS.Models;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;

namespace COMPASS.ViewModels
{
    public class ProgressViewModel : ObservableObject
    {

        #region Singleton pattern
        private ProgressViewModel() { }

        private static ProgressViewModel _progressVM;
        public static ProgressViewModel GetInstance() => _progressVM ??= new ProgressViewModel();

        #endregion

        private ObservableCollection<LogEntry> _log = new();
        public ObservableCollection<LogEntry> Log
        {
            get => _log;
            set => SetProperty(ref _log, value);
        }


        private int _counter;
        public int Counter
        {
            get => _counter;
            private set
            {
                SetProperty(ref _counter, value);
                RaisePropertyChanged(nameof(Percentage));
                RaisePropertyChanged(nameof(FullText));
                RaisePropertyChanged(nameof(ShowProgressBar));
            }
        }

        private int _totalAmount;
        public int TotalAmount
        {
            get => _totalAmount;
            set
            {
                SetProperty(ref _totalAmount, value);
                RaisePropertyChanged(nameof(Percentage));
                RaisePropertyChanged(nameof(FullText));
                RaisePropertyChanged(nameof(ShowProgressBar));
            }
        }

        public float Percentage
        {
            get
            {
                if (TotalAmount == 0) return 100;
                return Counter * 100 / TotalAmount;
            }
        }

        private string _text;
        public string Text
        {
            get => _text;
            set
            {
                SetProperty(ref _text, value);
                RaisePropertyChanged(nameof(FullText));
            }
        }

        public string FullText => $"{Text} [{Counter + 1} / {TotalAmount}]";

        public bool ShowProgressBar => TotalAmount > 0 && Counter < TotalAmount;

        private Mutex progressMutex = new();
        public void IncrementCounter()
        {
            progressMutex.WaitOne();
            Counter++;
            progressMutex.ReleaseMutex();
        }

        public void ResetCounter()
        {
            progressMutex.WaitOne();
            Counter = 0;
            progressMutex.ReleaseMutex();
        }

        public void AddLogEntry(LogEntry entry) =>
            Application.Current.Dispatcher.Invoke(() =>
            Log.Add(entry)
        );
    }
}
