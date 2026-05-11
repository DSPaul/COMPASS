using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Models.DragDrop;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.Avalonia.DragDrop;

namespace COMPASS.Common.Views.Main;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        FilterDragManager = new DragManager()
        .AddHandler(new DragHandler<Filter>
            {
                DataFormat = DataTransferFormats.FilterFormat,
                GetData = visual => (visual.DataContext as FilterViewModel)?.GetModel(),
            })
        .AddHandler(new DragHandler<Tag>
            {
                DataFormat = DataTransferFormats.TagFormat,
                GetData = visual => ((visual.DataContext as FilterViewModel)?.GetModel() as TagFilter)?.FilterValue is TagViewModel tagVm
                    ? tagVm.GetModel()
                    : null,
            })
        .OnClick(OnFilterClicked);
    }

    private CollectionTabVM? ActiveTabVM => DataContext switch
    {
        MainViewModel mainVm => mainVm.TabsVM.ActiveTab,
        TabsViewModel tabsVm => tabsVm.ActiveTab,
        CollectionTabVM tabVm => tabVm,
        _ => null
    };

    public DragManager FilterDragManager { get; }

    private async void UserControl_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.S:
                // Ctrl + S to search
                if (e.KeyModifiers == KeyModifiers.Control)
                {
                    Searchbar.Focus();
                    e.Handled = true;
                }
                break;

            case Key.I:
                // Ctrl + I toggle info
                if (e.KeyModifiers == KeyModifiers.Control && ActiveTabVM != null)
                {
                    ActiveTabVM.CurrentLayout.CodexInfoVM.ShowCodexInfo = !ActiveTabVM.CurrentLayout.CodexInfoVM.ShowCodexInfo;
                    e.Handled = true;
                }
                break;
            case Key.T:
                if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
                {
                    TabsViewModel.GetInstance().ReopenTab();
                    e.Handled = true;
                }
                else if (e.KeyModifiers == KeyModifiers.Control)
                {
                    TabsViewModel.GetInstance().CreateTab();
                    e.Handled = true;
                }
                break;

            case Key.F5:
                if (ActiveTabVM != null)
                {
                    await ActiveTabVM.Refresh();
                }
                e.Handled = true;
                break;
        }

    }

    private void LayoutSelection_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb &&
            ActiveTabVM != null &&
            e.AddedItems.Count > 0 &&
            e.AddedItems[0] is Layout layout)
        {
            ActiveTabVM.ChangeLayoutCommand.Execute(layout.LayoutType);
        }
    }

    private void OnFilterClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Visual visual &&
                visual.DataContext is FilterViewModel filterVm &&
                ActiveTabVM?.FiltersVM is FiltersViewModel filtersVm &&
                filtersVm.RemoveFilterCommand.CanExecute(filterVm))
        {
            filtersVm.RemoveFilterCommand.Execute(filterVm);
        }
    }
}