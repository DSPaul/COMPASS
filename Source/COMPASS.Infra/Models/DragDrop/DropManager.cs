using Avalonia.Controls;
using Avalonia.Input;

namespace COMPASS.Infra.Models.DragDrop
{
    /// <summary>
    /// Manages the drop interaction for different data types.
    /// </summary>
    public class DropManager
    {
        private List<DropHandler> DropHandlers { get; } = [];

        public IEnumerable<DropHandler> GetHandlers() => DropHandlers;

        public DropManager AddHandler(DropHandler handler)
        {
            DropHandlers.Add(handler);
            return this;
        }

        public DropManager AddHandler<T>(DropHandler<T> handler) where T : class
        {
            DropHandlers.Add(handler);
            return this;
        }

        /// <summary>
        /// Returns the first drop handler that can handle the transfer, or null if none can
        /// </summary>
        public DropHandler? GetFirstApplicableHandler(IDataTransfer transfer) => 
            DropHandlers.FirstOrDefault(c => c.CanHandleDrop(transfer));

        public bool CanHandleDrop(IDataTransfer transfer) =>
            DropHandlers.Any(dropHandler => dropHandler.CanHandleDrop(transfer));

        /// <summary>
        /// Executes the first handler that can handle the transfer with positional context.
        /// </summary>
        public bool HandleDrop(IDataTransfer transfer, DropContext context)
        {
            var handler = GetFirstApplicableHandler(transfer);
            if (handler != null) 
            { 
                return handler.TryHandleDrop(transfer, context);
            }
            return false;
        }

        /// <summary>
        /// Gets the adorner from the first applicable handler with positional context.
        /// </summary>
        public Control? GetAdorner(IDataTransfer transfer, DropContext context)
        {
            var handler = GetFirstApplicableHandler(transfer);
            return handler?.GetAdorner(transfer, context);
        }
    }
}


