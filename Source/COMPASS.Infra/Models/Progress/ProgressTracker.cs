using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Models.Measuring;
using COMPASS.Infra.Tools;
using COMPASS.Infra.Tools.Logging;
using SharpCompress.Common;
using System.Collections.ObjectModel;

namespace COMPASS.Infra.Models.Progress
{
    public class ProgressTracker : ObservableObject, IProgress<IProgressReport>, IProgress<ProgressReport>, ILogger
    {
        private readonly Lock _stateLock = new();
        private readonly SynchronizationContext? _notificationContext;

        private double _total;
        private double _progress;
        private string _statusMessage = "";

        public ProgressTracker(Quantity quantity)
        {
            Quantity = quantity;
            _total = double.MaxValue;

            //The UI context registered at startup, so reporting works no matter
            //which thread constructs the tracker or reports to it.
            _notificationContext = UiSynchronizationContext.Current;
        }

        /// <summary>
        /// The total amount of units to be processed.
        /// </summary>
        public double Total
        {
            get
            {
                lock (_stateLock)
                {
                    return _total;
                }
            }
            set => SetTotal(value);
        }

        /// <summary>
        /// The amount of units processed
        /// </summary>
        public double Progress
        {
            get
            {
                lock (_stateLock)
                {
                    return _progress;
                }
            }
            set => SetProgress(value);
        }

        public string StatusMessage
        {
            get
            {
                lock (_stateLock)
                {
                    return _statusMessage;
                }
            }
            set => SetStatusMessage(value);
        }

        public ObservableCollection<LogEntry> MessageLog { get; } = [];

        /// <summary>
        /// Upper bound for <see cref="MessageLog"/>; oldest entries are dropped first.
        /// Long-running tasks must not grow the log (and the UI bound to it) without limit.
        /// </summary>
        public int MaxLogEntries { get; set; } = 500;

        /// <summary>
        /// The quantity being tracked (e.g. file size, items)
        /// </summary>
        public Quantity Quantity { get; }

        public bool IsComplete
        {
            get
            {
                lock (_stateLock)
                {
                    return _progress >= _total;
                }
            }
        }

        /// <summary>
        /// Completed fraction between 0 and 1, or null when the total is unknown
        /// (indeterminate progress — bind spinners to <see cref="IsIndeterminate"/>).
        /// </summary>
        public double? Fraction
        {
            get
            {
                lock (_stateLock)
                {
                    if (_total >= double.MaxValue / 2 || _total <= 0) return null;
                    return Math.Clamp(_progress / _total, 0, 1);
                }
            }
        }

        /// <summary>
        /// Completed percentage between 0 and 100 for progress bars (0 while indeterminate).
        /// </summary>
        public double Percentage => (Fraction ?? 0) * 100;

        /// <summary>
        /// True while <see cref="Fraction"/> is null — bind indeterminate spinners to this.
        /// </summary>
        public bool IsIndeterminate => Fraction is null;

        public void Report(IProgressReport value)
        {
            switch (value)
            {
                case XOutOfYReport xOutOfYReport:
                    SetProgressAndTotal(xOutOfYReport.Value, xOutOfYReport.Total);
                    break;
                case FractionalReport fractionalReport:
                    SetProgressAndTotal(fractionalReport.Fraction, 1);
                    break;
                case IncrementReport:
                    IncrementProgress();
                    break;
                case CompletedReport:
                    CompleteProgress();
                    break;
                case StatusReport messageReport:
                    SetStatusMessage(messageReport.Status);
                    break;
                case LogReport logReport:
                    AppendLogMessage(logReport.LogMessage);
                    break;
            }
        }

        public void Report(ProgressReport value)
        {
            SetProgressAndTotal(value.BytesTransferred, value.TotalBytes ?? double.MaxValue);
        }

        private void SetTotal(double total)
        {
            bool changed;
            lock (_stateLock)
            {
                changed = _total != total;
                if (changed)
                {
                    _total = total;
                }
            }

            if (changed)
            {
                RaisePropertyChanged(nameof(Total));
                RaisePropertyChanged(nameof(Fraction));
                RaisePropertyChanged(nameof(Percentage));
                RaisePropertyChanged(nameof(IsIndeterminate));
                RaisePropertyChanged(nameof(IsComplete));
            }
        }

        private void SetProgress(double progress)
        {
            bool changed;
            lock (_stateLock)
            {
                changed = _progress != progress;
                if (changed)
                {
                    _progress = progress;
                }
            }

            if (changed)
            {
                RaisePropertyChanged(nameof(Progress));
                RaisePropertyChanged(nameof(Fraction));
                RaisePropertyChanged(nameof(Percentage));
                RaisePropertyChanged(nameof(IsIndeterminate));
                RaisePropertyChanged(nameof(IsComplete));
            }
        }

        private void SetProgressAndTotal(double progress, double total)
        {
            bool progressChanged;
            bool totalChanged;
            lock (_stateLock)
            {
                progressChanged = _progress != progress;
                totalChanged = _total != total;
                _progress = progress;
                _total = total;
            }

            if (totalChanged)
            {
                RaisePropertyChanged(nameof(Total));
                RaisePropertyChanged(nameof(Fraction));
                RaisePropertyChanged(nameof(Percentage));
                RaisePropertyChanged(nameof(IsIndeterminate));
            }

            if (progressChanged)
            {
                RaisePropertyChanged(nameof(Progress));
                RaisePropertyChanged(nameof(Fraction));
                RaisePropertyChanged(nameof(Percentage));
                RaisePropertyChanged(nameof(IsIndeterminate));
            }

            if (progressChanged || totalChanged)
            {
                RaisePropertyChanged(nameof(IsComplete));
            }
        }

        private void IncrementProgress()
        {
            lock (_stateLock)
            {
                _progress++;
            }

            RaisePropertyChanged(nameof(Progress));
            RaisePropertyChanged(nameof(Fraction));
            RaisePropertyChanged(nameof(Percentage));
            RaisePropertyChanged(nameof(IsIndeterminate));
            RaisePropertyChanged(nameof(IsComplete));
        }

        private void CompleteProgress()
        {
            double currentTotal;
            bool changed;
            lock (_stateLock)
            {
                currentTotal = _total;
                changed = _progress != currentTotal;
                _progress = currentTotal;
            }

            if (changed)
            {
                RaisePropertyChanged(nameof(Progress));
                RaisePropertyChanged(nameof(Fraction));
                RaisePropertyChanged(nameof(Percentage));
                RaisePropertyChanged(nameof(IsIndeterminate));
                RaisePropertyChanged(nameof(IsComplete));
            }
        }

        private void SetStatusMessage(string statusMessage)
        {
            bool changed;
            lock (_stateLock)
            {
                changed = _statusMessage != statusMessage;
                if (changed)
                {
                    _statusMessage = statusMessage;
                }
            }

            if (changed)
            {
                RaisePropertyChanged(nameof(StatusMessage));
            }
        }

        private void AppendLogMessage(LogEntry logMessage)
        {
            if (_notificationContext is not null && _notificationContext != SynchronizationContext.Current)
            {
                // ObservableCollection must be mutated on the UI thread
                _notificationContext.Post(_ => AddLogEntryOnNotificationThread(logMessage), null);
                return;
            }

            AddLogEntryOnNotificationThread(logMessage);
        }

        private void AddLogEntryOnNotificationThread(LogEntry logMessage)
        {
            lock (_stateLock)
            {
                MessageLog.Add(logMessage);
                while (MessageLog.Count > MaxLogEntries)
                {
                    MessageLog.RemoveAt(0);
                }
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            if (_notificationContext is not null && _notificationContext != SynchronizationContext.Current)
            {
                _notificationContext.Post(_ => OnPropertyChanged(propertyName), null);
                return;
            }

            OnPropertyChanged(propertyName);
        }

        #region ILogger
        public void Info(string message)
        {
            var logEntry = new LogEntry(Severity.Info, message);
            AppendLogMessage(logEntry);
        }

        //Don't show debug messages in the progress tracker, they are too verbose and not useful to the user
        public void Debug(string message, Exception? ex = null) {}
        public void Warn(string message, Exception? ex = null)
        {
            var logEntry = new LogEntry(Severity.Warning, message);
            AppendLogMessage(logEntry);
        }

        public void Error(string message, Exception ex)
        {
            var logEntry = new LogEntry(Severity.Error, message);
            AppendLogMessage(logEntry);
        }

        public void Fatal(string message, Exception ex) => Error(message, ex);

        #endregion
    }
}
