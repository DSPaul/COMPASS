using System.Collections.ObjectModel;

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
}
