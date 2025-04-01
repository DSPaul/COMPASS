using Autofac;
using Avalonia.Controls;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendClientAreaToDecorationsHint = true; //allows me to put stuff in title bar
    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        ProgressViewModel.GetInstance().CancelBackgroundTask();
        App.Container.Resolve<ICodexCollectionStorageService>().Save(MainViewModel.CollectionVM.CurrentCollection);
        PreferencesService.GetInstance().SavePreferences();
    }
}
