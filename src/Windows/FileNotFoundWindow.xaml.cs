using COMPASS.ViewModels;
using System.Windows;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for HandleBrokenPathWindow.xaml
    /// </summary>
    public partial class FileNotFoundWindow : Window
    {
        public FileNotFoundWindow(FileNotFoundViewModel fileNotFoundVM)
        {
            InitializeComponent();
            DataContext = fileNotFoundVM;
            fileNotFoundVM.CloseAction = new(Close);
            fileNotFoundVM.SetDialogResult = new(value => DialogResult = value);
        }
    }
}
