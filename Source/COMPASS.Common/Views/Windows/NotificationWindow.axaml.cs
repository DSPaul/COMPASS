using Avalonia.Controls;
using Avalonia.Interactivity;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;

namespace COMPASS.Common.Views.Windows;

public partial class NotificationWindow : Window
{
    /// <summary>
    /// //DO NOT USE, FOR DESIGNER ONLY
    /// </summary>
    public NotificationWindow()
    {
        throw new Exception("DO NOT USE PARAMETERLESS CONSTRUCTOR");
    }
    
    private readonly Notification _notification;
    public NotificationWindow(Notification notification)
    {
        InitializeComponent();
        _notification = notification;
        DataContext = notification;
    }

    private void SafeClose() => Dispatcher.Post(Close);

    private void CancelClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        _notification.Result = NotificationAction.Cancel;

        SafeClose();
    }

    private void DeclineClick(object sender, RoutedEventArgs routedEventArgs)
    {
        _notification.Result = NotificationAction.Decline;
        SafeClose();
    }

    private void ConfirmClick(object sender, RoutedEventArgs routedEventArgs)
    {
        _notification.Result = NotificationAction.Confirm;
        SafeClose();
    }
}