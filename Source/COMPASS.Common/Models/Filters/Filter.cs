using Avalonia.Media;
using System;

namespace COMPASS.Common.Models.Filters
{
    public abstract class Filter : IEquatable<Filter>
    {
        protected Filter(FilterType filterType, object? filterValue = null)
        {
            Type = filterType;
            FilterValue = filterValue;
        }

        public FilterType Type { get; init; }
        public object? FilterValue { get; init; }

        /// <summary>
        /// Allow multiple filters of this type to be active at once
        /// </summary>
        public bool AllowMultiple { get; init; }

        #region ITag
        public abstract Color BackgroundColor { get; }

        public abstract string Content { get; }
        #endregion

        public abstract bool Apply(Codex codex);


        #region IEquatable
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
        public static bool operator ==(Filter? lhs, Filter? rhs)
        {
            if (lhs is null)
            {
                return rhs is null; //if lhs is null, only equal if rhs is also null
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }
        public static bool operator !=(Filter? lhs, Filter? rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode() => Content.GetHashCode();

        #endregion

    }
}
