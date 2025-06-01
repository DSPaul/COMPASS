using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Tools;
using OpenQA.Selenium.BiDi.Modules.Script;

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

        public abstract void Copy(SourceMetaData source, SourceMetaData target);
        public abstract void Apply(SourceMetaData source, Codex target);

        /// <summary>
        /// Checks if the codex to evaluated has a newer value for the property than the reference
        /// </summary>
        /// <param name="toEvaluate"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public abstract bool HasNewValue(SourceMetaData toEvaluate, Codex reference);

        public override string ToString() => Label;
        #endregion

        #region Import Sources

        private List<MetaDataSourceType> _sourcePriority = [];
        /// <summary>
        /// Ordered List of sources that can set this prop, used for logic
        /// </summary>
        public List<MetaDataSourceType> SourcePriority
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
            nameof(Codex.Cover) => new CoverProperty(propName, label: "Cover Art"),
            nameof(Codex.Rating) => new NumberProperty<int>(propName),
            nameof(Codex.Sources.ISBN) => new StringProperty($"{nameof(Codex.Sources)}.{nameof(Codex.Sources.ISBN)}", label: "ISBN"),
            _ => null //could occur when a new preference file with new props is loaded into an older version of compass
        };

        public static List<MetaDataSourceType> GetDefaultSources(string propName) => propName switch
        {
            nameof(SourceMetaData.Title) => new()
                {
                    MetaDataSourceType.PDF,
                    MetaDataSourceType.File,
                    MetaDataSourceType.GmBinder,
                    MetaDataSourceType.Homebrewery,
                    MetaDataSourceType.GoogleDrive,
                    MetaDataSourceType.ISBN,
                    MetaDataSourceType.GenericURL
                },
            nameof(SourceMetaData.Authors) => new()
                {
                    MetaDataSourceType.PDF,
                    MetaDataSourceType.GmBinder,
                    MetaDataSourceType.Homebrewery,
                    MetaDataSourceType.ISBN,
                    MetaDataSourceType.GenericURL
                },
            nameof(SourceMetaData.Publisher) => new()
                {
                    MetaDataSourceType.ISBN,
                    MetaDataSourceType.GmBinder,
                    MetaDataSourceType.Homebrewery,
                    MetaDataSourceType.GoogleDrive,
                },
            nameof(SourceMetaData.Version) => new()
                {
                    MetaDataSourceType.Homebrewery
                },
            nameof(SourceMetaData.PageCount) => new()
                {
                    MetaDataSourceType.PDF,
                    MetaDataSourceType.Image,
                    MetaDataSourceType.GmBinder,
                    MetaDataSourceType.Homebrewery,
                    MetaDataSourceType.ISBN,
                },
            nameof(SourceMetaData.Tags) => new()
                {
                    MetaDataSourceType.File,
                    MetaDataSourceType.GenericURL,
                },
            nameof(SourceMetaData.Description) => new()
                {
                    MetaDataSourceType.Homebrewery,
                    MetaDataSourceType.ISBN,
                    MetaDataSourceType.GenericURL,
                },
            nameof(SourceMetaData.ReleaseDate) => new()
                {
                    MetaDataSourceType.Homebrewery,
                    MetaDataSourceType.ISBN,
                },
            nameof(SourceMetaData.Cover) => new()
                {
                    MetaDataSourceType.Image,
                    MetaDataSourceType.PDF,
                    MetaDataSourceType.GmBinder,
                    MetaDataSourceType.Homebrewery,
                    MetaDataSourceType.GoogleDrive,
                    MetaDataSourceType.ISBN,
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

        public override void Copy(SourceMetaData source, SourceMetaData target)
            => target.SetProperty(Name, GetProp(source));

        public override void Apply(SourceMetaData source, Codex target)
        {
            target.SetProperty(Name, GetProp(source));
        }

        public override bool HasNewValue(SourceMetaData toEvaluate, Codex reference) =>
            !EqualityComparer<T>.Default.Equals(GetProp(toEvaluate), GetProp(reference));
    }
}
