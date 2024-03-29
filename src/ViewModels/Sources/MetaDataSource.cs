﻿using COMPASS.Models;
using System;
using System.Windows.Media;

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
        public static bool operator ==(NamedMetaDataSource lhs, NamedMetaDataSource rhs)
        {
            if (lhs is null)
            {
                return rhs is null;  //if lhs is null, only equal if rhs is also null
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
