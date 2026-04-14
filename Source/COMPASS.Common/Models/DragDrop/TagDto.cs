using Avalonia.Media;

namespace COMPASS.Common.Models.DragDrop
{
    public class TagDto
    {
        public int ID { get; set; } = -1;

        public string Content { get; set; } = "";

        public Color BackgroundColor { get; set; }

        public bool IsGroup { get; set; }
    }
}
