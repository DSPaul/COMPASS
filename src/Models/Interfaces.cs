using System.Collections.ObjectModel;
using System.Windows.Media;

namespace COMPASS.Models
{
    public interface IHasID
    {
        public int ID { get; set; }
    }

    public interface IHasChilderen<T> where T : IHasChilderen<T>
    {
        public ObservableCollection<T> Children { get; set; }
    }

    //Object that can be represented by a Tag in the UI
    public interface ITag
    {
        public string Content { get; }
        public Color BackgroundColor { get; }
    }
}
