using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class ChooseMetaDataWindow : Window
{
    public ChooseMetaDataWindow(ChooseMetaDataViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}