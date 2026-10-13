using System.Collections.Specialized;
using COMPASS.Common.Shell;

namespace COMPASS.Common.Features.Logging;

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
