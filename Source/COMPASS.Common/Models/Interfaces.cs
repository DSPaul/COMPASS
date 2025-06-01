using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace COMPASS.Common.Models
{
    public interface IHasID
    {
        int ID { get; set; }
    }

    public interface IHasChildren<T> where T : IHasChildren<T>
    {
        ObservableCollection<T> Children { get; }
    }

    public interface IItemWrapper<T>
    {
        T Item { get; set; }
    }

    public interface IExpandable
    {
        bool Expanded { get; set; }
    }

    public interface IDealsWithTabControl
    {
        int SelectedTab { get; set; }
        bool Collapsed { get; set; }
        int PrevSelectedTab { get; set; }
    }

    public interface IHasCodexMetadata { }
}
