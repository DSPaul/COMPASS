using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Infra.Avalonia.DragDrop;
using COMPASS.Common.Shell;

namespace COMPASS.Common.Features.Filtering;

public partial class FiltersSidePanel : SidePanel
{
    public FiltersSidePanel()
    {
        InitializeComponent();

        var dragManager = new DragManager()
            .AddHandler(new DragHandler<Filter>
            {
                DataFormat = FilterDataTransfer.FilterFormat,
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
