using COMPASS.ViewModels;
using System;
using System.Windows;

namespace COMPASS
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
            ((TagEditViewModel)DataContext).CloseAction = new Action(this.Close);
        }
    }
}
