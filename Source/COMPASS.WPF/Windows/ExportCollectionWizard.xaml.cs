﻿using System.Windows;
using COMPASS.ViewModels;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ExportCollectionWizard.xaml
    /// </summary>
    public partial class ExportCollectionWizard : Window
    {
        public ExportCollectionWizard(ExportCollectionViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
            vm.CloseAction = Close;
            vm.CancelAction = () => DialogResult = false;
        }
    }
}
