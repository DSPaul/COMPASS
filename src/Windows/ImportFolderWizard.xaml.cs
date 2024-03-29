﻿using COMPASS.ViewModels.Import;
using System.Windows;


namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ImportFolderWizard.xaml
    /// </summary>
    public partial class ImportFolderWizard : Window
    {
        public ImportFolderWizard(ImportFolderViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.CloseAction = () => DialogResult = true;
            vm.CancelAction = () => DialogResult = false;
        }
    }
}
