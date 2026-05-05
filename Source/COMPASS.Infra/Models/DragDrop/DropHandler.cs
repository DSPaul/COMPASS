using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace COMPASS.Infra.Models.DragDrop
{
    public abstract class DropHandler
    {
        /// <summary>
        /// Returns the adorner to show during DragEnter/DragOver, or null if this config cannot handle the transfer.
        /// </summary>
        public abstract TemplatedControl? TryGetAdorner(IDataTransfer transfer);

        /// <summary>
        /// Returns true if the transfer can be handled by this config.
        /// </summary>
        public abstract bool CanHandleDrop(IDataTransfer transfer);

        /// <summary>
        /// Executes the drop. Returns true if the transfer was handled.
        /// </summary>
        public abstract bool TryHandleDrop(IDataTransfer transfer);

        /// <summary>
        /// Gets the effects to show during DragOver.
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        public DragDropEffects DropEffects { get; set; }
    }

    public class DropHandler<T> : DropHandler where T : class
    {
        public DropHandler(DataFormat<T> dataFormat, DragDropEffects dropEffects, Action<T> onDropped)
        {
            DataFormat = dataFormat;
            DropEffects = dropEffects;
            OnDropped = onDropped;
        }

        public DataFormat<T> DataFormat { get;  }
        public Action<T> OnDropped { get; }

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

        public override bool TryHandleDrop(IDataTransfer transfer)
        {
            var data = TryExtract(transfer);
            if (data is null) return false;
            OnDropped(data);
            return true;
        }
    }
}
