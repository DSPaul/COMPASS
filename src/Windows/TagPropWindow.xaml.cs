using COMPASS.ViewModels;
using System;
using System.Windows;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for TagPropWindow.xaml
    /// </summary>
    public partial class TagPropWindow : Window
    {
        public TagPropWindow(TagEditViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
            ((TagEditViewModel)DataContext).CloseAction = Close;
        }
    }
}
