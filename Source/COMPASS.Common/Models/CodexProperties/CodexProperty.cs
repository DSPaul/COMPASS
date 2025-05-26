using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Models.CodexProperties
{
    public abstract class CodexProperty : ObservableObject
    {
        protected CodexProperty(string propName, string? label = null)
        {
            Name = propName;
            Label = label ?? propName;
        }

        #region Properties

        public string Name { get; init; }

        public string Label { get; }

        #endregion

        #region Methods

        public abstract bool IsEmpty(IHasCodexMetadata codex);

        public abstract void SetProp(IHasCodexMetadata target, IHasCodexMetadata source);

        /// <summary>
        /// Checks if the codex to evaluated has a newer value for the property than the reference
        /// </summary>
        /// <param name="toEvaluate"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public abstract bool HasNewValue(IHasCodexMetadata toEvaluate, IHasCodexMetadata reference);

        public override string ToString() => Label;
        #endregion

        #region Import Sources

        private List<MetaDataSource> _sourcePriority = [];
        /// <summary>
        /// Ordered List of sources that can set this prop, used for logic
        /// </summary>
        public List<MetaDataSource> SourcePriority
        {
            get => _sourcePriority;
            set => SetProperty(ref _sourcePriority, value);
        }

        private MetaDataOverwriteMode _overwriteMode = MetaDataOverwriteMode.IfEmpty;
        public MetaDataOverwriteMode OverwriteMode
        {
            get => _overwriteMode;
            set => SetProperty(ref _overwriteMode, value);
        }

        #endregion

        #region Factory

        public static CodexProperty? GetInstance(string propName) => propName switch
        {
            nameof(Codex.Title) => new StringProperty(propName),
            nameof(Codex.Authors) => new EnumerableProperty<string>(propName),
            nameof(Codex.Publisher) => new StringProperty(propName),
            nameof(Codex.Version) => new StringProperty(propName),
            nameof(Codex.PageCount) => new NumberProperty<int>(propName, label: "Page count"),
            nameof(Codex.Tags) => new TagsProperty(propName),
            nameof(Codex.Description) => new StringProperty(propName),
            nameof(Codex.ReleaseDate) => new DateTimeProperty(propName, label: "Release Date"),
            nameof(Codex.CoverArtPath) => new CoverArtProperty(propName, label: "Cover Art"),
            nameof(Codex.Rating) => new NumberProperty<int>(propName),
            nameof(Codex.Sources.ISBN) => new StringProperty($"{nameof(Codex.Sources)}.{nameof(Codex.Sources.ISBN)}", label: "ISBN"),
            _ => null //could occur when a new preference file with new props is loaded into an older version of compass
        };

        public static List<MetaDataSource> GetDefaultSources(string propName) => propName switch
        {
            nameof(Codex.Title) => new()
                {
                    MetaDataSource.PDF,
                    MetaDataSource.File,
                    MetaDataSource.GmBinder,
                    MetaDataSource.Homebrewery,
                    MetaDataSource.GoogleDrive,
                    MetaDataSource.ISBN,
                    MetaDataSource.GenericURL
                },
            nameof(Codex.Authors) => new()
                {
                    MetaDataSource.PDF,
                    MetaDataSource.GmBinder,
                    MetaDataSource.Homebrewery,
                    MetaDataSource.ISBN,
                    MetaDataSource.GenericURL
                },
            nameof(Codex.Publisher) => new()
                {
                    MetaDataSource.ISBN,
                    MetaDataSource.GmBinder,
                    MetaDataSource.Homebrewery,
                    MetaDataSource.GoogleDrive,
                },
            nameof(Codex.Version) => new()
                {
                    MetaDataSource.Homebrewery
                },
            nameof(Codex.PageCount) => new()
                {
                    MetaDataSource.PDF,
                    MetaDataSource.Image,
                    MetaDataSource.GmBinder,
                    MetaDataSource.Homebrewery,
                    MetaDataSource.ISBN,
                },
            nameof(Codex.Tags) => new()
                {
                    MetaDataSource.File,
                    MetaDataSource.GenericURL,
                },
            nameof(Codex.Description) => new()
                {
                    MetaDataSource.Homebrewery,
                    MetaDataSource.ISBN,
                    MetaDataSource.GenericURL,
                },
            nameof(Codex.ReleaseDate) => new()
                {
                    MetaDataSource.Homebrewery,
                    MetaDataSource.ISBN,
                },
            nameof(Codex.CoverArtPath) => new()
                {
                    MetaDataSource.Image,
                    MetaDataSource.PDF,
                    MetaDataSource.GmBinder,
                    MetaDataSource.Homebrewery,
                    MetaDataSource.GoogleDrive,
                    MetaDataSource.ISBN,
                },
            _ => new(),
        };
        #endregion
    }

    public abstract class CodexProperty<T> : CodexProperty
    {
        protected CodexProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override bool IsEmpty(IHasCodexMetadata codex) => EqualityComparer<T>.Default.Equals(GetProp(codex), default);

        protected virtual T? GetProp(IHasCodexMetadata codex)
        {
            object? value = codex.GetDeepPropertyValue(Name);
            return value == null ? default : (T)value;
        }

        public override void SetProp(IHasCodexMetadata target, IHasCodexMetadata source)
            => target.SetProperty(Name, GetProp(source));

        public override bool HasNewValue(IHasCodexMetadata toEvaluate, IHasCodexMetadata reference) =>
            !EqualityComparer<T>.Default.Equals(GetProp(toEvaluate), GetProp(reference));
    }
}
