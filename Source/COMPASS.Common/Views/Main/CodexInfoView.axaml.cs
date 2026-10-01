using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Models.DragDrop;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.Avalonia.DragDrop;

namespace COMPASS.Common.Views.Main;

public partial class CodexInfoView : UserControl
{
    public CodexInfoView()
    {
        InitializeComponent();

        var tagDragManager = new DragManager()
            .AddHandler(new DragHandler<Models.Tag>
            {
                DataFormat = DataTransferFormats.TagFormat,
                GetData = visual => (visual.DataContext as TagViewModel)?.GetModel(),
                IsDraggable = tag => !tag.IsGroup,
            })
            .OnClick(OnTagClicked);

        DragBehavior.SetDragManager(TagsControl, tagDragManager);
    }

    private void OnTagClicked(object? sender, PointerPressedEventArgs pressedArgs)
    {
        if (sender is not Visual visual) return;
        if (visual.DataContext is not TagViewModel tagViewModel) return;
        if (DataContext is not CodexInfoViewModel codexInfoViewModel) return;
        if (codexInfoViewModel.AddTagFilterCommand.CanExecute(tagViewModel))
        {
            codexInfoViewModel.AddTagFilterCommand.Execute(tagViewModel);
        }
    }
}
