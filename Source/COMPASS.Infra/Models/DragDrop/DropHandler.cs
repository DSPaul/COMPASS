using Avalonia.Input;

namespace COMPASS.Infra.Models.DragDrop
{
    public class DropHandler
    {
        private List<DropConfig> DropConfigs { get; } = [];

        public IEnumerable<DropConfig> GetConfigs() => DropConfigs;

        public DropHandler AddConfig<T>(DropConfig<T> config) where T : class
        {
            DropConfigs.Add(config);
            return this;
        }

        /// <summary>
        /// Returns the first drop config that can handle the transfer, or null if none can
        /// </summary>
        public DropConfig? GetFirstApplicableConfig(IDataTransfer transfer) => 
            DropConfigs.FirstOrDefault(c => c.CanHandleDrop(transfer));

        public bool CanHandleDrop(IDataTransfer transfer) =>
            DropConfigs.Any(dropConfig => dropConfig.CanHandleDrop(transfer));

        /// <summary>
        /// Executes the first config that can handle the transfer. Returns true if one was found.
        /// </summary>
        public bool HandleDrop(IDataTransfer transfer)
        {
            foreach (var dropConfig in DropConfigs)
            {
                if (dropConfig.TryHandleDrop(transfer)) return true;
            }
            return false;
        }
    }
}

