using System;
using System.IO;
using System.Windows.Media;

namespace COMPASS.Models
{
    public sealed class Filter : ITag, IEquatable<Filter>
    {
        public Filter(FilterType filterType, object? filterValue = null)
        {
            Type = filterType;
            FilterValue = filterValue;
        }

        public enum FilterType
        {
            Tag,
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
            FileExtension,
            HasBrokenPath,
            HasISBN,
            Domain
        }

        public Func<Codex, bool> Method => Type switch
        {
            FilterType.Author => codex => FilterValue is string author && codex.Authors.Contains(author),
            FilterType.Publisher => codex => FilterValue is string publisher && codex.Publisher == publisher,
            FilterType.StartReleaseDate => codex => FilterValue is DateTime date && codex.ReleaseDate >= date,
            FilterType.StopReleaseDate => codex => FilterValue is DateTime date && codex.ReleaseDate < date,
            FilterType.MinimumRating => codex => FilterValue is int rating && codex.Rating >= rating,
            FilterType.OfflineSource => codex => codex.HasOfflineSource(),
            FilterType.OnlineSource => codex => codex.HasOnlineSource(),
            FilterType.HasISBN => codex => !String.IsNullOrEmpty(codex.ISBN),
            FilterType.PhysicalSource => codex => codex.PhysicallyOwned,
            FilterType.Favorite => codex => codex.Favorite,
            FilterType.FileExtension => codex => FilterValue is string extension && codex.FileType == extension,
            FilterType.HasBrokenPath => codex => codex.HasOfflineSource() && !Path.Exists(codex.Path),
            FilterType.Domain => codex => FilterValue is string domain && codex.HasOnlineSource() && codex.SourceURL.Contains(domain),
            _ => _ => true
        };

        //Implement ITag interface
        public string Content
        {
            get
            {
                if (FilterValue is null) return Label;
                string formattedFilterValue = FilterValue switch
                {
                    DateTime date => date.ToShortDateString(),
                    ITag tag => tag.Content,
                    _ => FilterValue.ToString() ?? string.Empty
                };
                return $"{Label} {formattedFilterValue} {Suffix}".Trim();
            }
        }

        public Color BackgroundColor => Type switch
        {
            FilterType.Author => Colors.Orange,
            FilterType.Publisher => Colors.MediumPurple,
            FilterType.StartReleaseDate => Colors.DeepSkyBlue,
            FilterType.StopReleaseDate => Colors.DeepSkyBlue,
            FilterType.MinimumRating => Colors.Goldenrod,
            FilterType.OfflineSource => Colors.DarkSeaGreen,
            FilterType.OnlineSource => Colors.DarkSeaGreen,
            FilterType.PhysicalSource => Colors.DarkSeaGreen,
            FilterType.HasISBN => Colors.DarkSeaGreen,
            FilterType.Favorite => Colors.HotPink,
            FilterType.FileExtension => Colors.OrangeRed,
            FilterType.Search => Colors.Salmon,
            FilterType.Tag => ((ITag?)FilterValue)!.BackgroundColor,
            FilterType.HasBrokenPath => Colors.Gold,
            FilterType.Domain => Colors.MediumTurquoise,
            _ => throw new NotImplementedException(),
        };

        //Properties
        public FilterType Type { get; init; }
        public object? FilterValue { get; init; }
        public string Label => Type switch
        {
            FilterType.Author => "Author:",
            FilterType.Publisher => "Publisher:",
            FilterType.StartReleaseDate => "After:",
            FilterType.StopReleaseDate => "Before:",
            FilterType.MinimumRating => "At least",
            FilterType.OfflineSource => "Available Offline",
            FilterType.OnlineSource => "Available Online",
            FilterType.PhysicalSource => "Physically Owned",
            FilterType.Favorite => "Favorite",
            FilterType.FileExtension => "File Type:",
            FilterType.Search => "Search:",
            FilterType.HasBrokenPath => "Has Broken Path",
            FilterType.HasISBN => "Has ISBN",
            FilterType.Domain => "From:",
            _ => "",
        };
        public string Suffix => Type switch
        {
            FilterType.MinimumRating => "stars",
            _ => ""
        };
        public bool Unique => Type switch
        {
            FilterType.StartReleaseDate => true,
            FilterType.StopReleaseDate => true,
            FilterType.MinimumRating => true,
            FilterType.Search => true,
            FilterType.HasBrokenPath => true,
            _ => false
        };

        //Overwrite Equal operator
        public override bool Equals(object? obj) => Equals(obj as Filter);

        public bool Equals(Filter? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (GetType() != other.GetType())
                return false;
            return Type == other.Type && FilterValue == other.FilterValue;
        }
        public static bool operator ==(Filter lhs, Filter rhs)
        {
            if (lhs is null)
            {
                return rhs is null; //if lhs is null, only equal if rhs is also null
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
