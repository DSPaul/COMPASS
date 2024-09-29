using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common;

public partial class ChooseMetaDataWindow : Window
{
    public ChooseMetaDataWindow(ChooseMetaDataViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}