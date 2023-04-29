using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Xml.Serialization;

namespace COMPASS.ViewModels.Sources
{
    [Flags]
    public enum ImportSource
    {
        None = 0,
        Manual = 1,
        File = 2,
        Folder = 4,
        GmBinder = 8,
        Homebrewery = 16,
        DnDBeyond = 32,
        GoogleDrive = 64,
        Dropbox = 128,
        ISBN = 256,
        GenericURL = 512
    }

    public class NamedImportSource : ITag
    {
        // Parameterless ctor for serialization
        public NamedImportSource() { }

        public NamedImportSource(ImportSource source)
        {
            Source = source;
        }

        private Dictionary<ImportSource, string> ImportSourceNames = new()
        {
            { ImportSource.None, "None"},
            { ImportSource.Manual, "Manual"},
            { ImportSource.File,"Local File"},
            { ImportSource.Folder, "Folder"},
            { ImportSource.GmBinder,"GM Binder"},
            { ImportSource.Homebrewery,"Homebrewery"},
            { ImportSource.GoogleDrive,"Google Drive"},
            { ImportSource.ISBN, "Open Library (ISBN)"},
            { ImportSource.GenericURL,"Website Header" }
        };

        public ImportSource Source;

        // ITag Interface
        [XmlIgnore]
        public string Content => ImportSourceNames[Source];
        [XmlIgnore]
        public Color BackgroundColor => throw new NotImplementedException();

        //Overwrite Equal operator
        public override bool Equals(object obj) => Equals(obj as NamedImportSource);

        public bool Equals(NamedImportSource other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (GetType() != other.GetType())
                return false;
            return Source == other.Source;
        }
        public static bool operator ==(NamedImportSource lhs, NamedImportSource rhs)
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
        public static bool operator !=(NamedImportSource lhs, NamedImportSource rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode() => Source.GetHashCode();
    }
}
