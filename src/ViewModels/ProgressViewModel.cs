using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Models;
using System.Collections.ObjectModel;
using System.Threading;

namespace COMPASS.ViewModels
{
    public class ProgressViewModel : ObservableObject
    {

        #region Singleton pattern
        private ProgressViewModel() { }

        private static ProgressViewModel? _progressVM;
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
                OnPropertyChanged(nameof(Percentage));
                OnPropertyChanged(nameof(FullText));
                OnPropertyChanged(nameof(WorkInProgress));
            }
        }

        private int _totalAmount;
        public int TotalAmount
        {
            get => _totalAmount;
            set
            {
                if (value == _totalAmount) return;
                SetProperty(ref _totalAmount, value);
                OnPropertyChanged(nameof(Percentage));
                OnPropertyChanged(nameof(FullText));
                OnPropertyChanged(nameof(WorkInProgress));
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

        /// <summary>
        /// Displays [x/y] next to export title
        /// </summary>
        public bool ShowCount { get; set; } = true;

        private string _text = "";
        public string Text
        {
            get => _text;
            set
            {
                SetProperty(ref _text, value);
                OnPropertyChanged(nameof(FullText));
            }
        }

        public string FullText
        {
            get
            {
                if (Cancelling) return $"Cancelling {Text}...";
                string result = $"{Text}";
                if (ShowCount) result += $" [{Counter} / {TotalAmount}]";
                return result;
            }
        }

        public bool WorkInProgress => TotalAmount > 0 && Counter < TotalAmount;

        private readonly Mutex _progressMutex = new();
        public void IncrementCounter()
        {
            _progressMutex.WaitOne();
            Counter++;
            _progressMutex.ReleaseMutex();
        }

        public void ResetCounter()
        {
            _progressMutex.WaitOne();
            Counter = 0;
            _progressMutex.ReleaseMutex();
        }

        public void Clear()
        {
            ResetCounter();
            TotalAmount = 0;
        }

        public void AddLogEntry(LogEntry entry) =>
            App.SafeDispatcher.Invoke(() =>
            Log.Add(entry)
        );

        public bool Cancelling { get; set; } = false;
        public static CancellationTokenSource GlobalCancellationTokenSource { get; private set; } = new();
        public void ConfirmCancellation()
        {
            //Reset any progress
            Clear();
            //create a new tokenSource
            GlobalCancellationTokenSource = new();
            //force refresh the command so that it grabs the right cancel function
            _cancelTasksCommand = null;
            OnPropertyChanged(nameof(CancelTasksCommand));
            Cancelling = false;
        }

        private RelayCommand? _cancelTasksCommand;
        public RelayCommand CancelTasksCommand => _cancelTasksCommand ??= new(CancelBackgroundTask);
        public void CancelBackgroundTask()
        {
            GlobalCancellationTokenSource.Cancel();
            Cancelling = true;
            OnPropertyChanged(nameof(FullText));
        }
    }
}
