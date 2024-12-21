using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class CodexBulkEditWindow : Window
{
    public CodexBulkEditWindow(CodexBulkEditViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}