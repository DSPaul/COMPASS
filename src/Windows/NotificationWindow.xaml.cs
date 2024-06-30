using COMPASS.Models;
using System.Windows;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        private Notification _notification;
        public NotificationWindow(Notification notification)
        {
            InitializeComponent();

            _notification = notification;
            DataContext = notification;
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            _notification.Result = Models.Enums.NotificationAction.Cancel;
            DialogResult = false;
        }

        private void DeclineClick(object sender, RoutedEventArgs e)
        {
            _notification.Result = Models.Enums.NotificationAction.Decline;
            DialogResult = true;
        }

        private void ConfirmClick(object sender, RoutedEventArgs e)
        {
            _notification.Result = Models.Enums.NotificationAction.Confirm;
            DialogResult = true;
        }
    }
}
