using Avalonia.Controls;
using Avalonia.Interactivity;
using COMPASS.Common.Models;

namespace COMPASS.Common.Views.Windows;

public partial class NotificationWindow : Window
{
    private Notification _notification;
    public NotificationWindow(Notification notification)
    {
        InitializeComponent();

        _notification = notification;
        DataContext = notification;
    }

    private void CancelClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        _notification.Result = Models.Enums.NotificationAction.Cancel;
        Close();
    }

    private void DeclineClick(object sender, RoutedEventArgs routedEventArgs)
    {
        _notification.Result = Models.Enums.NotificationAction.Decline;
        Close();
    }

    private void ConfirmClick(object sender, RoutedEventArgs routedEventArgs)
    {
        _notification.Result = Models.Enums.NotificationAction.Confirm;
        Close();
    }
}