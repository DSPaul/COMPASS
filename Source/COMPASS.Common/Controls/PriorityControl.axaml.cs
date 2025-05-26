using System.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace COMPASS.Common.Controls;

public partial class PriorityControl : ItemsControl
{
    public PriorityControl()
    {
        InitializeComponent();
    }

    private void MoveUp(object? sender, RoutedEventArgs e)
    {
        if (ItemsSource is not IList items) return;

        var btn = sender as Control;
        object? toMove = btn?.DataContext;
        if (toMove is null) return;

        int i = items.IndexOf(toMove);
        if (i <= 0) return;

        //move the items
        items.RemoveAt(i);
        items.Insert(i - 1, toMove);
        
        UpdateUI();
    }

    private void MoveDown(object? sender, RoutedEventArgs e)
    {
        if (ItemsSource is not IList items) return;

        var btn = sender as Control;
        object? toMove = btn?.DataContext;
        if (toMove is null) return;

        int i = items.IndexOf(toMove);
        if (i + 1 >= items.Count) return;

        //move the items
        items.RemoveAt(i);
        items.Insert(i + 1, toMove);
        
        UpdateUI();
    }


    private void UpdateUI()
    {
        // Force ItemsControl to refresh
        var temp = ItemsSource;
        ItemsSource = null;
        ItemsSource = temp;
    }
    
    //key to uniquely identify control so drag drop only works within the same control
    public static int Key { get; set; } = 0;
}