using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using COMPASS.Common.ViewModels.Modals;

namespace COMPASS.Common.Views;

public partial class ToolsView : UserControl
{
    public ToolsView()
    {
        InitializeComponent();
        DataContext = new ToolsViewModel();
    }
}