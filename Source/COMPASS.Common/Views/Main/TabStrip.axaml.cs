using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Common.Views.Main;

public partial class TabStrip : UserControl
{
    public TabStrip()
    {
        InitializeComponent();
    }
    
    private async void CollectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //only refresh if the selection changes from one collection to another
        //so both an added and removed collection
        if (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0 && DataContext is TabsViewModel tabsVM)
        {
            await tabsVM.ActiveTab!.ChangeToCollection(e.AddedItems.Cast<CodexCollectionVM>().First());
        }
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Properties.IsMiddleButtonPressed && sender is Control {DataContext: CollectionTabVM tabVM})
        {
            TabsViewModel.GetInstance().CloseTab(tabVM);
        }
    }
}