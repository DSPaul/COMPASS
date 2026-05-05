using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Models.DragDrop;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.Behaviors;
using COMPASS.Infra.Models.DragDrop;

namespace COMPASS.Common.Views.SidePanels;

public partial class FiltersSidePanel : SidePanel
{
    public FiltersSidePanel()
    {
        InitializeComponent();

        var dragManager = new DragManager()
            .AddHandler(new DragHandler<COMPASS.Common.Models.Filters.Filter>
            {
                DataFormat = DataTransferFormats.FilterFormat,
                GetData = visual => (visual?.DataContext as FilterViewModel)?.GetModel(),
            })
            .OnClick(OnFilterChipClicked);

        DragBehavior.SetDragManager(BooleanFiltersControl, dragManager);
    }

    private void OnFilterChipClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Avalonia.Visual visual) return;
        if (visual.DataContext is not FilterViewModel filterViewModel) return;
        if (DataContext is not FiltersViewModel filtersViewModel) return;
        filtersViewModel.ActivateFilterCommand.Execute(filterViewModel);
    }

    private void ClearOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb)
        {
            cb.Clear();
        }
    }
}