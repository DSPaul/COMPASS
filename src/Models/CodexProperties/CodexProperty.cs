﻿using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Models.XmlDtos;
using COMPASS.Tools;
using COMPASS.ViewModels.Sources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.Models.CodexProperties
{
    public abstract class CodexProperty : ObservableObject
    {
        protected CodexProperty(string propName, string? label = null)
        {
            Name = propName;
            Label = label ?? propName;
            _defaultSourcePriority = GetDefaultSources(propName);
        }

        #region Properties

        public string Name { get; init; }

        public string Label { get; init; }

        #endregion

        #region Methods

        public abstract bool IsEmpty(Codex codex);

        public abstract void SetProp(Codex target, Codex source);

        /// <summary>
        /// Checks if the codex to evaluated has a newer value for the property than the reference
        /// </summary>
        /// <param name="toEvaluate"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public abstract bool HasNewValue(Codex toEvaluate, Codex reference);

        #endregion

        #region Import Sources

        private readonly List<MetaDataSource> _defaultSourcePriority;

        private ObservableCollection<NamedMetaDataSource>? _sourcePriorityNamed;
        /// <summary>
        /// Ordered List of sources that can set this prop, named for data binding
        /// </summary>
        public ObservableCollection<NamedMetaDataSource> SourcePriorityNamed
        {
            get => _sourcePriorityNamed ??= new(SourcePriority.Select(source => new NamedMetaDataSource(source)));
            set => SourcePriority = value.Select(namedSource => namedSource.Source).ToList();
        }

        private List<MetaDataSource> _sourcePriority = new();
        /// <summary>
        /// Ordered List of sources that can set this prop, used for logic
        /// </summary>
        public List<MetaDataSource> SourcePriority
        {
            get => _sourcePriority;
            set
            {
                _sourcePriority = value;
                UpdateSources();
            }
        }

        private MetaDataOverwriteMode _overwriteMode = MetaDataOverwriteMode.IfEmpty;
        public MetaDataOverwriteMode OverwriteMode
        {
            get => _overwriteMode;
            set => SetProperty(ref _overwriteMode, value);
        }

        #endregion

        #region Mapping

        public void UpdateSources()
        {
            // If a new possible source was not found in the save, add it
            foreach (var source in _defaultSourcePriority)
            {
                SourcePriority.AddIfMissing(source);
            }

            // If a possible source was removed (due to a specific metadata fetch breaking
            // or due to an api change or something), remove it from the sources
            SourcePriority.RemoveAll(source => !_defaultSourcePriority.Contains(source));
        }

        public CodexPropertyDto ToDto()
        {
            CodexPropertyDto dto = new()
            {
                Name = Name,
                OverwriteMode = OverwriteMode,
                //Use order from NameMetaDataSources (which was reordered by user)
                SourcePriority = SourcePriorityNamed.Select(namedSource => namedSource.Source).ToList(),
            };

            return dto;
        }

        #endregion

        #region Factory

        public static CodexProperty? GetInstance(string propName) => propName switch
        {
            nameof(Codex.Title) => new StringProperty(nameof(Codex.Title)),
            nameof(Codex.Authors) => new EnumerableProperty<string>(nameof(Codex.Authors)),
            nameof(Codex.Publisher) => new StringProperty(nameof(Codex.Publisher)),
            nameof(Codex.Version) => new StringProperty(nameof(Codex.Version)),
            nameof(Codex.PageCount) => new NumberProperty<int>(nameof(Codex.PageCount), label: "Pagecount"),
            nameof(Codex.Tags) => new TagsProperty(nameof(Codex.Tags)),
            nameof(Codex.Description) => new StringProperty(nameof(Codex.Description)),
            nameof(Codex.ReleaseDate) => new DateTimeProperty(nameof(Codex.ReleaseDate), label: "Release Date"),
            nameof(Codex.CoverArt) => new CoverArtProperty(nameof(Codex.CoverArt), label: "Cover Art"),
            _ => null //could occur when a new preference file with new props is loaded into an older version of compass
        };

        public static CodexProperty? FromDto(CodexPropertyDto propDto)
        {
            var prop = GetInstance(propDto.Name);

            if (prop == null)
            {
                return null;
            }

            prop.SourcePriority = propDto.SourcePriority;
            prop.OverwriteMode = propDto.OverwriteMode;

            return prop;
        }

        private static List<MetaDataSource> GetDefaultSources(string propName) => propName switch
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
            nameof(Codex.CoverArt) => new()
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

    public class CodexProperty<T> : CodexProperty
    {
        public CodexProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override bool IsEmpty(Codex codex) => EqualityComparer<T>.Default.Equals(GetProp(codex), default);

        public T? GetProp(Codex codex)
        {
            object? value = codex.GetPropertyValue(Name);
            return value == null ? default : (T)value;
        }

        public override void SetProp(Codex target, Codex source)
            => target.SetProperty(Name, GetProp(source));

        public override bool HasNewValue(Codex toEvaluate, Codex reference) =>
            !EqualityComparer<T>.Default.Equals(GetProp(toEvaluate), GetProp(reference));
    }
}