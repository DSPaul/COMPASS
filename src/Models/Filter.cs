using System;
using System.Windows.Media;

namespace COMPASS.Models
{
    public sealed class Filter : ITag, IEquatable<Filter>
    {
        public Filter(FilterType filtertype, object filterValue = null)
        {
            Type = filtertype;
            FilterValue = filterValue;
        }

        public enum FilterType
        {
            Search,
            Author,
            Publisher,
            StartReleaseDate,
            StopReleaseDate,
            MinimumRating,
            OnlineSource,
            OfflineSource,
            PhysicalSource,
            Favorite,
            FileExtension
        }

        public Func<Codex, bool> Method => Type switch
        {
            FilterType.Author => new(codex => codex.Authors.Contains((string)FilterValue)),
            FilterType.Publisher => new(codex => codex.Publisher == (string)FilterValue),
            FilterType.StartReleaseDate => new(codex => codex.ReleaseDate >= (DateTime?)FilterValue),
            FilterType.StopReleaseDate => new(codex => codex.ReleaseDate < (DateTime?)FilterValue),
            FilterType.MinimumRating => new(codex => codex.Rating >= (int?)FilterValue),
            FilterType.OfflineSource => new(codex => codex.HasOfflineSource()),
            FilterType.OnlineSource => new(codex => codex.HasOnlineSource()),
            FilterType.PhysicalSource => new(codex => codex.Physically_Owned),
            FilterType.Favorite => new(codex => codex.Favorite),
            FilterType.FileExtension => new(codex => codex.GetFileType() == (string)FilterValue),
            _ => new(_ => true)
        };

        //Implement ITag interface
        public string Content
        {
            get
            {
                if (FilterValue is null)
                {
                    return Label;
                }

                string formatedFilterValue = FilterValue switch
                {
                    DateTime date => date.ToShortDateString(),
                    _ => FilterValue.ToString()
                };
                return $"{Label} {formatedFilterValue} {Suffix}".Trim();
            }
        }
        public Color BackgroundColor { get; set; }

        //Properties
        public FilterType Type { get; init; }
        public object FilterValue { get; init; }
        public string Label { get; set; }
        public string Suffix { get; set; } = "";
        public bool Unique { get; set; } = false;

        //Overwrite Equal operator
        public override bool Equals(object obj) => this.Equals(obj as Filter);

        public bool Equals(Filter other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (this.GetType() != other.GetType())
                return false;
            return Content == other.Content;
        }
        public static bool operator ==(Filter lhs, Filter rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }
        public static bool operator !=(Filter lhs, Filter rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode() => Content.GetHashCode();
    }
}
