using System;
using System.IO;
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
            HasBrokenPath
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
            FilterType.HasBrokenPath => new(codex => codex.HasOfflineSource() && !Path.Exists(codex.Path)),
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
                    ITag tag => tag.Content,
                    _ => FilterValue.ToString()
                };
                return $"{Label} {formatedFilterValue} {Suffix}".Trim();
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
            FilterType.Favorite => Colors.HotPink,
            FilterType.FileExtension => Colors.OrangeRed,
            FilterType.Search => Colors.Salmon,
            FilterType.Tag => ((ITag)FilterValue).BackgroundColor,
            FilterType.HasBrokenPath => Colors.Gold,
            _ => throw new NotImplementedException(),
        };

        //Properties
        public FilterType Type { get; init; }
        public object FilterValue { get; init; }
        public string Label => Type switch
        {
            FilterType.Author => "Author:",
            FilterType.Publisher => "Publisher",
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
        public override bool Equals(object obj) => Equals(obj as Filter);

        public bool Equals(Filter other)
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
