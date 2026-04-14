using Avalonia.Controls.Primitives;
using COMPASS.Common.Models.DragDrop;

namespace COMPASS.Common.Adorners
{
    public class DropTagAdorner : TemplatedControl
    {
        public DropTagAdorner(TagDto draggedTag)
        {
            DataContext = draggedTag;
        }
    }
}
