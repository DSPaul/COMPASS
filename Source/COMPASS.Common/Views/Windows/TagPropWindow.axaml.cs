using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common;

public partial class TagPropWindow : Window
{
    public TagPropWindow(TagEditViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}