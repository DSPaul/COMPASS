using System;
using static COMPASS.Tools.Enums;

namespace COMPASS.Models
{
    public class FilterTag : Tag
    {
        public FilterTag() : base() { }
        public FilterTag(FilterType filtertype, object filterValue = null)
        {
            _filterType = filtertype;
            FilterValue = filterValue;
        }

        readonly FilterType _filterType;
        public object FilterValue { get; init; }
        public string Label;
        public string Suffix = "";
        public bool Unique = false;
        public override string Content
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

        public override object GetGroup() => _filterType;

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj is FilterTag objAsTag && Equals(objAsTag);
        }

        public bool Equals(FilterTag other)
        {
            if (other == null) return false;
            return (Content.Equals(other.Content));
        }

        public override int GetHashCode()
        {
            return Content.GetHashCode();
        }
    }
}
