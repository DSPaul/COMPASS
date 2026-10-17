using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Features.Tags;
using COMPASS.Infra.Avalonia.DragDrop;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Codices;

public partial class CodexInfoView : UserControl
{
    public CodexInfoView()
    {
        InitializeComponent();

        var tagDragManager = new DragManager()
            .AddHandler(new DragHandler<Tag>
            {
                DataFormat = TagDataTransfer.TagFormat,
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
