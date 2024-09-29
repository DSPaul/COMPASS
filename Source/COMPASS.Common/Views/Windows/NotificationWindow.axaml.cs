using Avalonia.Controls;
using COMPASS.Common.Models;

namespace COMPASS.Common;

public partial class NotificationWindow : Window
{
    public NotificationWindow(Notification notification)
    {
        InitializeComponent();
        DataContext = notification;
    }
}