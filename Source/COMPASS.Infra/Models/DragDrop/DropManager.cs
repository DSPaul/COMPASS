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
        /// Executes the first handler that can handle the transfer. Returns true if one was found.
        /// </summary>
        public bool HandleDrop(IDataTransfer transfer)
        {
            foreach (var dropHandler in DropHandlers)
            {
                if (dropHandler.TryHandleDrop(transfer)) return true;
            }
            return false;
        }
    }
}

