using System.Collections.Specialized;

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