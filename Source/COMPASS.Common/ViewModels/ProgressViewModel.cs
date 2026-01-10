using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using SharpCompress.Common;

namespace COMPASS.Common.ViewModels
{
    public class ProgressViewModel : ViewModelBase, IProgress<ProgressReport>
    {

        #region Singleton pattern
        private ProgressViewModel() { }

        private static ProgressViewModel? _progressVM;
        public static ProgressViewModel GetInstance() => _progressVM ??= new ProgressViewModel();

        #endregion

        public ObservableCollection<LogEntry> Log
        {
            get;
            set => SetProperty(ref field, value);
        } = [];


        public int Counter
        {
            get;
            private set
            {
                SetProperty(ref field, value);
                OnPropertyChanged(nameof(Percentage));
                OnPropertyChanged(nameof(FullText));
                OnPropertyChanged(nameof(WorkInProgress));
            }
        }

        public int TotalAmount
        {
            get;
            set
            {
                if (value == field) return;
                SetProperty(ref field, value);
                OnPropertyChanged(nameof(Percentage));
                OnPropertyChanged(nameof(FullText));
                OnPropertyChanged(nameof(WorkInProgress));
            }
        }

        public int Percentage
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

        public string Text
        {
            get;
            set
            {
                SetProperty(ref field, value);
                OnPropertyChanged(nameof(FullText));
            }
        } = "";

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

        public void Report(ProgressReport report)
        {
            if (report.PercentComplete != null)
            {
                TotalAmount = 100;
                Counter = (int)report.PercentComplete;
            }
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
            Dispatcher.UIThread.Invoke(() =>
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
