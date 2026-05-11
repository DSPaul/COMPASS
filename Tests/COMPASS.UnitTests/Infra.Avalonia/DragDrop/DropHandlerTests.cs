using Avalonia.Input;
using Border = global::Avalonia.Controls.Border;
using COMPASS.Infra.Avalonia.DragDrop;
using COMPASS.Infra.Avalonia.ExtensionMethods;

namespace COMPASS.UnitTests.Infra.Avalonia.DragDrop;

[TestFixture]
public class DropHandlerTests
{
    private static readonly DataFormat<string> TestFormat = DataFormat.CreateInProcessFormat<string>("TestDropFormat");

    private static IDataTransfer CreateTransferWith(params string[] items)
    {
        var transfer = new DataTransfer();
        foreach (var item in items)
        {
            transfer.AddData(TestFormat, item);
        }
        return transfer;
    }

    private static IDataTransfer CreateEmptyTransfer() => new DataTransfer();

    #region CanHandleDrop

    [Test]
    public void CanHandleDrop_MatchingData_ReturnsTrue()
    {
        var handler = new DropHandler<string>(TestFormat, DragDropEffects.Copy);
        var transfer = CreateTransferWith("item1");

        bool canHandle = handler.CanHandleDrop(transfer);

        Assert.That(canHandle, Is.True);
    }

    [Test]
    public void CanHandleDrop_NoMatchingData_ReturnsFalse()
    {
        var handler = new DropHandler<string>(TestFormat, DragDropEffects.Copy);
        var transfer = CreateEmptyTransfer();

        bool canHandle = handler.CanHandleDrop(transfer);

        Assert.That(canHandle, Is.False);
    }

    [Test]
    public void CanHandleDrop_CanDropFilterRejectsAll_ReturnsFalse()
    {
        var handler = new DropHandler<string>(TestFormat, DragDropEffects.Copy)
        {
            CanDrop = _ => false
        };
        var transfer = CreateTransferWith("item1");

        bool canHandle = handler.CanHandleDrop(transfer);

        Assert.That(canHandle, Is.False);
    }

    [Test]
    public void CanHandleDrop_CanDropFilterAcceptsSome_ReturnsTrue()
    {
        var handler = new DropHandler<string>(TestFormat, DragDropEffects.Copy)
        {
            CanDrop = item => item == "good"
        };
        var transfer = CreateTransferWith("bad", "good");

        bool canHandle = handler.CanHandleDrop(transfer);

        Assert.That(canHandle, Is.True);
    }

    #endregion

    #region OnDroppedSingle Callback

    [Test]
    public void TryHandleDrop_OnDroppedSingle_InvokesForEachItem()
    {
        var droppedItems = new List<string>();
        var handler = new DropHandler<string>(TestFormat, DragDropEffects.Copy)
        {
            OnDroppedSingle = item => droppedItems.Add(item)
        };
        var transfer = CreateTransferWith("a", "b");
        var context = new DropContext
        {
            DropTarget = new Border(),
            DragEventArgs = null!
        };

        bool handled = handler.TryHandleDrop(transfer, context);

        Assert.That(handled, Is.True);
        Assert.That(droppedItems, Is.EquivalentTo(["a", "b"]));
    }

    #endregion

    #region OnDroppedMultiple Callback

    [Test]
    public void TryHandleDrop_OnDroppedMultiple_InvokedWithAllItems()
    {
        string[]? receivedItems = null;
        var handler = new DropHandler<string>(TestFormat, DragDropEffects.Copy)
        {
            OnDroppedMultiple = items => receivedItems = items
        };
        var transfer = CreateTransferWith("x", "y", "z");
        var context = new DropContext
        {
            DropTarget = new Border(),
            DragEventArgs = null!
        };

        bool handled = handler.TryHandleDrop(transfer, context);

        Assert.That(handled, Is.True);
        Assert.That(receivedItems, Is.EquivalentTo(["x", "y", "z"]));
    }

    #endregion

    #region OnDroppedMultipleAsync takes precedence

    [Test]
    public void TryHandleDrop_AsyncCallbackSet_TakesPrecedenceOverSingle()
    {
        bool asyncCalled = false;
        bool singleCalled = false;
        var handler = new DropHandler<string>(TestFormat, DragDropEffects.Copy)
        {
            OnDroppedMultipleAsync = _ => { asyncCalled = true; return Task.CompletedTask; },
            OnDroppedSingle = _ => singleCalled = true
        };
        var transfer = CreateTransferWith("item");
        var context = new DropContext
        {
            DropTarget = new Border(),
            DragEventArgs = null!
        };

        handler.TryHandleDrop(transfer, context);

        Assert.That(asyncCalled, Is.True);
        Assert.That(singleCalled, Is.False);
    }

    #endregion

    #region No callback returns false

    [Test]
    public void TryHandleDrop_NoCallbacksSet_ReturnsFalse()
    {
        var handler = new DropHandler<string>(TestFormat, DragDropEffects.Copy);
        var transfer = CreateTransferWith("item");
        var context = new DropContext
        {
            DropTarget = new Border(),
            DragEventArgs = null!
        };

        bool handled = handler.TryHandleDrop(transfer, context);

        Assert.That(handled, Is.False);
    }

    #endregion

    #region CanDrop filter applied during drop

    [Test]
    public void TryHandleDrop_CanDropFilters_OnlyDropsAcceptedItems()
    {
        var droppedItems = new List<string>();
        var handler = new DropHandler<string>(TestFormat, DragDropEffects.Copy)
        {
            CanDrop = item => item.StartsWith("ok"),
            OnDroppedSingle = item => droppedItems.Add(item)
        };
        var transfer = CreateTransferWith("ok1", "bad", "ok2");
        var context = new DropContext
        {
            DropTarget = new Border(),
            DragEventArgs = null!
        };

        handler.TryHandleDrop(transfer, context);

        Assert.That(droppedItems, Is.EquivalentTo(["ok1", "ok2"]));
    }

    #endregion

    #region Empty transfer

    [Test]
    public void TryHandleDrop_EmptyTransfer_ReturnsFalse()
    {
        var handler = new DropHandler<string>(TestFormat, DragDropEffects.Copy)
        {
            OnDroppedSingle = _ => { }
        };
        var transfer = CreateEmptyTransfer();
        var context = new DropContext
        {
            DropTarget = new Border(),
            DragEventArgs = null!
        };

        bool handled = handler.TryHandleDrop(transfer, context);

        Assert.That(handled, Is.False);
    }

    #endregion
}
