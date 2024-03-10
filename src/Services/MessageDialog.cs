using COMPASS.Interfaces;
using System.Windows;

namespace COMPASS.Services
{
    public class MessageDialog : IMessageBox
    {
        public MessageBoxResult Show(string message, string title, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
            => MessageBox.Show(message, title, buttons, icon);
    }
}
