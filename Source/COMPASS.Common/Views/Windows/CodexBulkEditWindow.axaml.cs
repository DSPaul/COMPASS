using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common;

public partial class CodexBulkEditWindow : Window
{
    public CodexBulkEditWindow(CodexBulkEditViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}