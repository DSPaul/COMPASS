using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace COMPASS.Infra.Models.DragDrop
{
    public abstract class DropHandler
    {
        /// <summary>
        /// Gets the effects to show during DragOver.
        /// </summary>
        public DragDropEffects DropEffects { get; set; }

        /// <summary>
        /// Returns true if the transfer can be handled by this handler.
        /// </summary>
        public abstract bool CanHandleDrop(IDataTransfer transfer);

        /// <summary>
        /// Returns the adorner to show during DragOver, or null for no adorner.
        /// Override the context-aware overload for position-dependent adorners.
        /// </summary>
        public virtual Control? GetAdorner(IDataTransfer transfer, DropContext context) => TryGetAdorner(transfer);

        /// <summary>
        /// Simple adorner factory without positional context. Override <see cref="GetAdorner"/> for position-aware adorners.
        /// </summary>
        public virtual TemplatedControl? TryGetAdorner(IDataTransfer transfer) => null;

        /// <summary>
        /// Simple drop handler without positional context. Override <see cref="HandleDrop"/> for position-aware drops.
        /// </summary>
        public virtual bool TryHandleDrop(IDataTransfer transfer, DropContext context) => false;
    }

    public class DropHandler<T> : DropHandler where T : class
    {
        public DropHandler(DataFormat<T> dataFormat, DragDropEffects dropEffects, Action<T>? onDropped)
        {
            DataFormat = dataFormat;
            DropEffects = dropEffects;
            OnDropped = onDropped;
        }

        public DataFormat<T> DataFormat { get; }
        public Action<T>? OnDropped { get; }

        /// <summary>
        /// Factory that creates the adorner to preview the drop. Receives the dragged item.
        /// </summary>
        public Func<T, TemplatedControl>? AdornerFactory { get; init; }

        /// <summary>
        /// Check certain conditions on the item to see if it can be dropped here.
        /// </summary>
        public Func<T, bool>? CanDrop { get; init; }

        private T? TryExtract(IDataTransfer transfer)
        {
            var data = transfer.TryGetValue(DataFormat);
            if (data is null) return null;
            if (CanDrop?.Invoke(data) == false) return null;
            return data;
        }

        public override TemplatedControl? TryGetAdorner(IDataTransfer transfer)
        {
            var data = TryExtract(transfer);
            if (data is null) return null;
            return AdornerFactory?.Invoke(data);
        }

        public override bool CanHandleDrop(IDataTransfer transfer) => TryExtract(transfer) is not null;

        public override bool TryHandleDrop(IDataTransfer transfer, DropContext context)
        {
            var data = TryExtract(transfer);
            if (data is null || OnDropped == null) return false;
            OnDropped(data);
            return true;
        }
    }
}
