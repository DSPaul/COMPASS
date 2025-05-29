using System;
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
            var addedLog = ActivityLog.LastOrDefault();
            HighestUnseenSeverity = addedLog.Severity > HighestUnseenSeverity ? addedLog.Severity : HighestUnseenSeverity;
            ShowAttention |= HighestUnseenSeverity != Severity.Info;
        };
    }
    
    public static ObservableCollection<LogEntry> ActivityLog { get; } = [];
    
    //Used for attention indicator
    private Severity _highestUnseenSeverity = Severity.Info;

    public Severity HighestUnseenSeverity
    {
        get => _highestUnseenSeverity;
        set => SetProperty(ref _highestUnseenSeverity, value);
    }

    private bool _showAttention;
    public bool ShowAttention 
    {
        get => _showAttention;
        set => SetProperty(ref _showAttention, value);
    }
}