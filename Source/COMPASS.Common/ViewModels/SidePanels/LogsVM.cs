using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.ViewModels.SidePanels;

public class LogsVM : ViewModelBase
{
    public static void AddLog(LogEntry log)
    {
        Dispatcher.UIThread.Invoke(() => ActivityLog.Add(log));
    }

    public LogsVM()
    {
        ActivityLog.CollectionChanged += (_,_) =>
        {
            OnPropertyChanged(nameof(LastSeverity));
            OnPropertyChanged(nameof(ShowAttention));
        };
    }
    
    public static ObservableCollection<LogEntry> ActivityLog { get; } = [];
    
    //Used for attention indicator
    public Severity LastSeverity => ActivityLog.LastOrDefault().Severity;
    public bool ShowAttention => LastSeverity != Severity.Info;
}