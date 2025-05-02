using System;
using System.Collections.ObjectModel;

namespace COMPASS.Common.Models
{
    public interface IHasID
    {
        public int ID { get; set; }
    }

    public interface IHasChildren<T> where T : IHasChildren<T>
    {
        public ObservableCollection<T> Children { get; }
    }

    public interface IItemWrapper<T>
    {
        public T Item { get; set; }
    }

    public interface IExpandable
    {
        public bool Expanded { get; set; }
    }

    public interface IDealsWithTabControl
    {
        public int SelectedTab { get; set; }
        public bool Collapsed { get; set; }
        public int PrevSelectedTab { get; set; }
    }

    public interface IHasCodexMetadata
    {
        #region COMPASS related Metadata
        public int ID { get; set; }
        public string CoverArtPath { get; set; }
        public string ThumbnailPath { get; set; }
        #endregion

        #region Codex related Metadata

        public string Title { get; set; }
        public string SortingTitle { get; set; }

        //public IList<string> Authors { get; set; }

        public string Publisher { get; set; }
        public string Description { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int PageCount { get; set; }
        public string Version { get; set; }

        #endregion

        #region User related Metadata
        public bool PhysicallyOwned { get; set; }
        public int Rating { get; set; }
        public bool Favorite { get; set; }

        #endregion

    }
}
