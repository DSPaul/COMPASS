using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.Models.CodexProperties
{
    /// <summary>
    /// Provides functionality to set the property from metata sources, and enables some filter logic
    /// </summary>
    public abstract class CodexProperty : ObservableObject
    {
        protected CodexProperty(string propName, string? label = null)
        {
            Name = propName;
            Label = label ?? propName;

            SourcePriority = GetDefaultSources(propName);
        }

        #region Properties

        public string Name { get; init; }

        public string Label { get; }

        private List<MetadataSourceType> _sourcePriority = [];
        /// <summary>
        /// Ordered List of sources that can set this prop, used for logic
        /// </summary>
        public List<MetadataSourceType> SourcePriority
        {
            get => _sourcePriority;
            set => SetProperty(ref _sourcePriority, value);
        }

        private MetadataOverwriteMode _overwriteMode = MetadataOverwriteMode.IfEmpty;
        public MetadataOverwriteMode OverwriteMode
        {
            get => _overwriteMode;
            set => SetProperty(ref _overwriteMode, value);
        }        
        #endregion

        #region Methods

        public abstract bool IsEmpty(IHasCodexMetadata codex);

        public abstract void Copy(SourceMetadata source, SourceMetadata target);
        public abstract void Apply(SourceMetadata source, Codex target);

        /// <summary>
        /// Checks if the codex to evaluated has a newer value for the property than the reference
        /// </summary>
        /// <param name="toEvaluate"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public abstract bool HasNewValue(SourceMetadata toEvaluate, Codex reference);

        public override string ToString() => Label;
        #endregion

        #region Factory

        public static CodexProperty? GetInstance(string propName) => propName switch
        {
            nameof(CodexViewModel.Title) => new StringProperty(propName),
            nameof(CodexViewModel.Authors) => new EnumerableProperty<string>(propName),
            nameof(CodexViewModel.Publisher) => new StringProperty(propName),
            nameof(CodexViewModel.Version) => new StringProperty(propName),
            nameof(CodexViewModel.PageCount) => new NumberProperty<int>(propName, label: "Page count"),
            nameof(CodexViewModel.Tags) => new TagsProperty(propName),
            nameof(CodexViewModel.Description) => new StringProperty(propName),
            nameof(CodexViewModel.ReleaseDate) => new DateTimeProperty(propName, label: "Release Date"),
            nameof(CodexViewModel.Cover) => new CoverProperty(propName, label: "Cover Art"),
            nameof(CodexViewModel.Rating) => new NumberProperty<int>(propName),
            nameof(SourceSet.ISBN) => new StringProperty($"{nameof(CodexViewModel.Sources)}.{nameof(Codex.Sources.ISBN)}", label: "ISBN"),
            _ => null //could occur when a new preference file with new props is loaded into an older version of compass
        };

        public static List<MetadataSourceType> GetDefaultSources(string propName) => propName switch
        {
            nameof(SourceMetadata.Title) => new()
                {
                    MetadataSourceType.PDF,
                    MetadataSourceType.File,
                    MetadataSourceType.GmBinder,
                    MetadataSourceType.Homebrewery,
                    MetadataSourceType.GoogleDrive,
                    MetadataSourceType.ISBN,
                    MetadataSourceType.GenericURL
                },
            nameof(SourceMetadata.Authors) => new()
                {
                    MetadataSourceType.PDF,
                    MetadataSourceType.GmBinder,
                    MetadataSourceType.Homebrewery,
                    MetadataSourceType.ISBN,
                    MetadataSourceType.GenericURL
                },
            nameof(SourceMetadata.Publisher) => new()
                {
                    MetadataSourceType.ISBN,
                    MetadataSourceType.GmBinder,
                    MetadataSourceType.Homebrewery,
                    MetadataSourceType.GoogleDrive,
                },
            nameof(SourceMetadata.Version) => new()
                {
                    MetadataSourceType.Homebrewery
                },
            nameof(SourceMetadata.PageCount) => new()
                {
                    MetadataSourceType.PDF,
                    MetadataSourceType.Image,
                    MetadataSourceType.GmBinder,
                    MetadataSourceType.Homebrewery,
                    MetadataSourceType.ISBN,
                },
            nameof(SourceMetadata.Tags) => new()
                {
                    MetadataSourceType.File,
                    MetadataSourceType.GenericURL,
                },
            nameof(SourceMetadata.Description) => new()
                {
                    MetadataSourceType.Homebrewery,
                    MetadataSourceType.ISBN,
                    MetadataSourceType.GenericURL,
                },
            nameof(SourceMetadata.ReleaseDate) => new()
                {
                    MetadataSourceType.Homebrewery,
                    MetadataSourceType.ISBN,
                },
            nameof(SourceMetadata.Cover) => new()
                {
                    MetadataSourceType.Image,
                    MetadataSourceType.PDF,
                    MetadataSourceType.GmBinder,
                    MetadataSourceType.Homebrewery,
                    MetadataSourceType.GoogleDrive,
                    MetadataSourceType.ISBN,
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

        public override void Copy(SourceMetadata source, SourceMetadata target)
            => target.SetProperty(Name, GetProp(source));

        public override void Apply(SourceMetadata source, Codex target)
        {
            target.SetProperty(Name, GetProp(source));
        }

        public override bool HasNewValue(SourceMetadata toEvaluate, Codex reference) =>
            !EqualityComparer<T>.Default.Equals(GetProp(toEvaluate), GetProp(reference));
    }
}
