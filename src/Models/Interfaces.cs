using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace COMPASS.Models
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

    public interface IHasCodexMetadata
    {
        #region COMPASS related Metadata
        public int ID { get; set; }
        public string CoverArt { get; set; }
        public string Thumbnail { get; set; }
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
