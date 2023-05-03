using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Xml.Serialization;

namespace COMPASS.ViewModels.Sources
{
    [Flags]
    public enum MetaDataSource
    {
        None = 0,
        File = 1,
        PDF = 2,
        Image = 4,
        GmBinder = 8,
        Homebrewery = 16,
        DnDBeyond = 32,
        GoogleDrive = 64,
        Dropbox = 128,
        ISBN = 256,
        GenericURL = 512,
    }

    public static class MetaDataSources
    {
        public static readonly MetaDataSource OnlineSources =
            MetaDataSource.GmBinder |
            MetaDataSource.Homebrewery |
            MetaDataSource.DnDBeyond |
            MetaDataSource.GoogleDrive |
            MetaDataSource.Dropbox |
            MetaDataSource.GenericURL;

        public static readonly MetaDataSource OfflineSources =
            MetaDataSource.File |
            MetaDataSource.PDF |
            MetaDataSource.Image;
    }

    public class NamedMetaDataSource : ITag
    {
        // Parameterless ctor for serialization
        public NamedMetaDataSource() { }

        public NamedMetaDataSource(MetaDataSource source)
        {
            Source = source;
        }

        private Dictionary<MetaDataSource, string> ImportSourceNames = new()
        {
            { MetaDataSource.None, "None"},
            { MetaDataSource.File,"File Name/Path"},
            { MetaDataSource.PDF, "PDF File"},
            { MetaDataSource.Image, "Image File"},
            { MetaDataSource.GmBinder,"GM Binder"},
            { MetaDataSource.Homebrewery,"Homebrewery"},
            { MetaDataSource.GoogleDrive,"Google Drive"},
            { MetaDataSource.ISBN, "Open Library (ISBN)"},
            { MetaDataSource.GenericURL,"Website Header" }
        };

        public MetaDataSource Source;

        // ITag Interface
        [XmlIgnore]
        public string Content => ImportSourceNames[Source];
        [XmlIgnore]
        public Color BackgroundColor => throw new NotImplementedException();

        //Overwrite Equal operator
        public override bool Equals(object obj) => Equals(obj as NamedMetaDataSource);

        public bool Equals(NamedMetaDataSource other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (GetType() != other.GetType())
                return false;
            return Source == other.Source;
        }
        public static bool operator ==(NamedMetaDataSource lhs, NamedMetaDataSource rhs)
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
        public static bool operator !=(NamedMetaDataSource lhs, NamedMetaDataSource rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode() => Source.GetHashCode();
    }
}
