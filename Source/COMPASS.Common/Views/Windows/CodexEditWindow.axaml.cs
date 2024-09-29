using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common;

public partial class CodexEditWindow : Window
{
    public CodexEditWindow(CodexEditViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}