using System.Windows;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        public LoadingWindow(string text)
        {
            InitializeComponent();
            LoadingTextBlock.Text = text;
        }
    }
}
