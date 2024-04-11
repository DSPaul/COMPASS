using System.Windows;

namespace COMPASS.Interfaces
{
    public interface IMessageBox
    {
        public MessageBoxResult Show(string message, string title, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None);
    }
}
