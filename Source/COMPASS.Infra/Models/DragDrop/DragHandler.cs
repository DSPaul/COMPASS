using Avalonia;
using Avalonia.Input;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Infra.Models.DragDrop
{
    public abstract class DragHandler
    {
        public abstract void TryAddToTransfer(DataTransfer transfer, Visual source);
    }

    public class DragHandler<T> : DragHandler where T : class
    {
        public required DataFormat<T> DataFormat { get; set; }

        public required Func<Visual, T?> GetData { get; set; }

        /// <summary>
        /// Check certain conditions on the item to see if it is draggable
        /// </summary>
        public Func<T, bool>? IsDraggable { get; set; }

        public Func<T>? OnDropped { get; set; }

        public override void TryAddToTransfer(DataTransfer transfer, Visual source)
        {
            var data = GetData(source);
            if (data != null && (IsDraggable?.Invoke(data) ?? true))
            {
                transfer.AddData(DataFormat, data);
            }
        }
    }
}