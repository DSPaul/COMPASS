using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class CodexEditWindow : Window
{
    public CodexEditWindow(CodexEditViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}