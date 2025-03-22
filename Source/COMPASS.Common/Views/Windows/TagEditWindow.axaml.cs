using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class TagEditWindow : Window
{
    public TagEditWindow(TagEditViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}