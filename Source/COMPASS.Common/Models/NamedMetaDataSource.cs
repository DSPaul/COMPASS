using Avalonia.Media;
using System;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.Models
{
    public class NamedMetaDataSource : ITag
    {
        // Parameterless ctor for serialization
        public NamedMetaDataSource() { }

        public NamedMetaDataSource(MetaDataSource source)
        {
            Source = source;
        }

        public MetaDataSource Source { get; init; }

        // ITag Interface
        public string Content => Source switch
        {
            MetaDataSource.None => "None",
            MetaDataSource.File => "File Name/Path",
            MetaDataSource.PDF => "PDF File",
            MetaDataSource.Image => "Image File",
            MetaDataSource.GmBinder => "GM Binder",
            MetaDataSource.Homebrewery => "Homebrewery",
            MetaDataSource.GoogleDrive => "Google Drive",
            MetaDataSource.ISBN => "Open Library (ISBN)",
            MetaDataSource.GenericURL => "Website Header",
            MetaDataSource.Dropbox => "Dropbox",
            MetaDataSource.DnDBeyond => "Dnd Beyond",
            _ => throw new NotImplementedException(),
        };

        public Color BackgroundColor => Colors.Transparent;

        //Overwrite Equal operator
        public override bool Equals(object? obj) => Equals(obj as NamedMetaDataSource);

        public bool Equals(NamedMetaDataSource? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (GetType() != other.GetType())
                return false;
            return Source == other.Source;
        }
        public static bool operator ==(NamedMetaDataSource? lhs, NamedMetaDataSource? rhs)
        {
            if (lhs is null)
            {
                return rhs is null;  //if lhs is null, only equal if rhs is also null
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }
        public static bool operator !=(NamedMetaDataSource? lhs, NamedMetaDataSource? rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode() => Source.GetHashCode();
    }
}
