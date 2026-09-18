using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Interactivity;
using COMPASS.Common.Services.StateManagers;

namespace COMPASS.Common.Views.Windows;

public partial class ProgressWindow : Window
{
    private readonly TrackedOperation _operation;

    public ProgressWindow(TrackedOperation operation)
    {
        _operation = operation ?? throw new ArgumentNullException(nameof(operation));
        DataContext = operation;
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

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Cancel_Click(object sender, RoutedEventArgs e) => _operation.Cancel();
}
