using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace COMPASS.Infra.Avalonia.DragDrop
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
        public DropHandler(DataFormat<T> dataFormat, DragDropEffects dropEffects)
        {
            DataFormat = dataFormat;
            DropEffects = dropEffects;
        }

        public DataFormat<T> DataFormat { get; }

        /// <summary>
        /// Callback invoked once per dropped item.
        /// </summary>
        public Action<T>? OnDroppedSingle { get; init; }

        /// <summary>
        /// Callback invoked with all dropped items at once.
        /// </summary>
        public Action<T[]>? OnDroppedMultiple { get; init; }

        /// <summary>
        /// Async callback invoked with all dropped items at once. Takes precedence over <see cref="OnDroppedSingle"/> and <see cref="OnDroppedMultiple"/> when set.
        /// </summary>
        public Func<T[], Task>? OnDroppedMultipleAsync { get; init; }

        /// <summary>
        /// Factory that creates the adorner to preview the drop. Receives all dropped items.
        /// </summary>
        public Func<T[], TemplatedControl>? AdornerFactory { get; init; }

        /// <summary>
        /// Check certain conditions on an item to see if it can be dropped here.
        /// </summary>
        public Func<T, bool>? CanDrop { get; init; }

        private T[]? TryExtract(IDataTransfer transfer)
        {
            var items = transfer.TryGetValues(DataFormat);
            if (items is null || items.Length == 0) return null;
            if (CanDrop is not null)
                items = items.Where(item => CanDrop(item)).ToArray();
            return items.Length > 0 ? items : null;
        }

        public override TemplatedControl? TryGetAdorner(IDataTransfer transfer)
        {
            var items = TryExtract(transfer);
            if (items is null) return null;
            return AdornerFactory?.Invoke(items);
        }

        public override bool CanHandleDrop(IDataTransfer transfer) => TryExtract(transfer) is not null;

        public override bool TryHandleDrop(IDataTransfer transfer, DropContext context)
        {
            var items = TryExtract(transfer);
            if (items is null) return false;

            if (OnDroppedMultipleAsync is not null)
            {
                OnDroppedMultipleAsync(items); // fire-and-forget
                return true;
            }

            if (OnDroppedMultiple is not null)
            {
                OnDroppedMultiple(items);
                return true;
            }

            if (OnDroppedSingle is not null)
            {
                foreach (var item in items)
                    OnDroppedSingle(item);
                return true;
            }

            return false;
        }
    }
}
