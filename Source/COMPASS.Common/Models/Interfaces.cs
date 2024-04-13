using Avalonia.Media;
using System.Collections.ObjectModel;

namespace COMPASS.Common.Models
{
    public interface IHasID
    {
        public int ID { get; set; }
    }

    public interface IHasChildren<T> where T : IHasChildren<T>
    {
        public ObservableCollection<T> Children { get; set; }
    }

    //Object that can be represented by a Tag in the UI
    public interface ITag
    {
        public string Content { get; }
        public Color BackgroundColor { get; }
    }

    public interface IDealsWithTabControl
    {
        public int SelectedTab { get; set; }
        public bool Collapsed { get; set; }
    }
}
