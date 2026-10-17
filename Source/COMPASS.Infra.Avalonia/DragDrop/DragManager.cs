using Avalonia.Input;

namespace COMPASS.Infra.Avalonia.DragDrop
{
    /// <summary>
    /// Manages the drag interactions for a control
    /// </summary>
    public class DragManager
    {
        private List<DragHandler> DragHandlers { get; } = [];
        public Action<object?, PointerPressedEventArgs>? ClickHandler { get; private set; }

        public IEnumerable<DragHandler> GetHandlers() => DragHandlers;

        public DragManager AddHandler(DragHandler handler)
        {
            DragHandlers.Add(handler);
            return this;
        }

        public DragManager AddHandler<T>(DragHandler<T> handler) where T : class
        {
            DragHandlers.Add(handler);
            return this; //builder pattern
        }

        public DragManager OnClick(Action<object?, PointerPressedEventArgs> handler)
        {
            ClickHandler = handler;
            return this;
        }
    }
}
