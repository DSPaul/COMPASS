using Avalonia.Input;

namespace COMPASS.Infra.Models.DragDrop
{
    public class DragHandler
    {
        private List<DragConfig> DragConfigs { get; } = [];
        public Action<object?, PointerPressedEventArgs>? ClickHandler { get; private set; }

        public IEnumerable<DragConfig> GetConfigs() => DragConfigs;
        public DragHandler AddConfig<T>(DragConfig<T> config) where T : class
        {
            DragConfigs.Add(config);
            return this; //builder pattern
        }

        public DragHandler OnClick(Action<object?, PointerPressedEventArgs> handler)
        {
            ClickHandler = handler;
            return this;
        }
    }
}
