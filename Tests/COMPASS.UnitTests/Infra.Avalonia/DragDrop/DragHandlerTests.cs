using Avalonia.Input;
using Border = global::Avalonia.Controls.Border;
using Control = global::Avalonia.Controls.Control;
using COMPASS.Infra.Avalonia.DragDrop;
using COMPASS.Infra.Avalonia.ExtensionMethods;

namespace COMPASS.UnitTests.Infra.Avalonia.DragDrop;

[TestFixture]
public class DragHandlerTests
{
    private static readonly DataFormat<string> TestFormat = DataFormat.CreateInProcessFormat<string>("TestDragFormat");

    [Test]
    public void TryAddToTransfer_DataFound_AddsToTransfer()
    {
        var handler = new DragHandler<string>
        {
            DataFormat = TestFormat,
            GetData = _ => "hello"
        };
        var transfer = new DataTransfer();
        var source = new Border();

        handler.TryAddToTransfer(transfer, source);

        Assert.That(transfer.TryGetValue(TestFormat), Is.EqualTo("hello"));
    }

    [Test]
    public void TryAddToTransfer_DataIsNull_DoesNotAddToTransfer()
    {
        var handler = new DragHandler<string>
        {
            DataFormat = TestFormat,
            GetData = _ => null
        };
        var transfer = new DataTransfer();
        var source = new Border();

        handler.TryAddToTransfer(transfer, source);

        Assert.That(transfer.TryGetValue(TestFormat), Is.Null);
    }

    [Test]
    public void TryAddToTransfer_IsDraggableReturnsFalse_DoesNotAddToTransfer()
    {
        var handler = new DragHandler<string>
        {
            DataFormat = TestFormat,
            GetData = _ => "hello",
            IsDraggable = _ => false
        };
        var transfer = new DataTransfer();
        var source = new Border();

        handler.TryAddToTransfer(transfer, source);

        Assert.That(transfer.TryGetValue(TestFormat), Is.Null);
    }

    [Test]
    public void TryAddToTransfer_IsDraggableReturnsTrue_AddsToTransfer()
    {
        var handler = new DragHandler<string>
        {
            DataFormat = TestFormat,
            GetData = _ => "draggable",
            IsDraggable = _ => true
        };
        var transfer = new DataTransfer();
        var source = new Border();

        handler.TryAddToTransfer(transfer, source);

        Assert.That(transfer.TryGetValue(TestFormat), Is.EqualTo("draggable"));
    }

    [Test]
    public void TryAddToTransfer_PassesSourceToGetData()
    {
        var expectedSource = new Border { Tag = "marker" };
        Control? receivedSource = null;
        var handler = new DragHandler<string>
        {
            DataFormat = TestFormat,
            GetData = v =>
            {
                receivedSource = v as Control;
                return "data";
            }
        };
        var transfer = new DataTransfer();

        handler.TryAddToTransfer(transfer, expectedSource);

        Assert.That(receivedSource, Is.SameAs(expectedSource));
    }
}
