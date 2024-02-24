using COMPASS.Interfaces;
using System.Windows;

namespace Tests.Mocks
{
    internal class MockMessageBox : IMessageBox
    {
        public MessageBoxResult Show(string message, string title, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
        {
            Console.WriteLine(message);
            return MessageBoxResult.OK;
        }
    }
}
