using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using COMPASS.Common.Models.Enums;
using ExCSS;
using OpenQA.Selenium;
using LogEntry = COMPASS.Common.Models.LogEntry;

namespace COMPASS.Common.Views.SidePanels;

public partial class LogsSidePanel : SidePanel
{
    public LogsSidePanel()
    {
        InitializeComponent();
        ((INotifyCollectionChanged)LogsControl.Items).CollectionChanged += Logs_CollectionChanged;
    }

    private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            // scroll the new item into view   
            Scroller.ScrollToEnd();
        }
    }
}