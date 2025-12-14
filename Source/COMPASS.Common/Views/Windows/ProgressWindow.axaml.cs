using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class ProgressWindow : Window
{
    public ProgressWindow(int bars = 1)
    {
        DataContext = ProgressViewModel.GetInstance();
        _totalBars = bars;
        InitializeComponent();
        ((INotifyCollectionChanged)LogsControl.Items).CollectionChanged += Logs_CollectionChanged;
    }

    private int _barsDone = 0;
    private int _totalBars;

    private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            // scroll the new item into view   
            Scroller.ScrollToEnd();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ProgBar_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (ProgBar.Value >= 100 || _barsDone >= _totalBars)
        {
            Close();
        }
    }
}