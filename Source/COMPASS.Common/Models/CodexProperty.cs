using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Sources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.Common.Models
{
    public class CodexProperty : ObservableObject
    {
        public CodexProperty(string propName, Func<Codex, bool> isEmpty, Action<Codex, Codex> setProp, List<MetaDataSource> defaultSources, string? label = null)
        {
            Name = propName;
            Label = label ?? propName;
            IsEmpty = isEmpty;
            SetProp = setProp;
            DefaultSourcePriority = defaultSources;
        }

        public CodexProperty(CodexPropertyDto dto, CodexProperty defaultProp)
        {
            Name = dto.Name;
            SourcePriority = dto.SourcePriority;
            OverwriteMode = dto.OverwriteMode;

            Label = defaultProp.Label;
            IsEmpty = defaultProp.IsEmpty;
            SetProp = defaultProp.SetProp;
            DefaultSourcePriority = defaultProp.DefaultSourcePriority;

            UpdateSources();
        }

        public string Name { get; init; } = "";

        public string Label { get; init; } = "";

        private Func<Codex, bool>? _isEmpty;
        public Func<Codex, bool> IsEmpty
        {
            get => _isEmpty ??= Codex.Properties.First(prop => prop.Name == Name).IsEmpty;
            private init => _isEmpty = value;
        }

        private Func<Codex, object?>? _getProp;
        public Func<Codex, object?> GetProp => _getProp ??= codex => codex.GetPropertyValue(Name);

        private Action<Codex, Codex>? _setProp;
        public Action<Codex, Codex> SetProp
        {
            get => _setProp ??= Codex.Properties.First(prop => prop.Name == Name).SetProp;
            private init => _setProp = value;
        }

        #region Import Sources
        private List<MetaDataSource>? _defaultSources;
        private List<MetaDataSource> DefaultSourcePriority
        {
            get => _defaultSources ??= Codex.Properties.First(prop => prop.Name == Name).DefaultSourcePriority;
            init => _defaultSources = value;
        }

        private ObservableCollection<NamedMetaDataSource>? _sourcePriorityNamed;
        /// <summary>
        /// Ordered List of sources that can set this prop, named for data binding
        /// </summary>
        public ObservableCollection<NamedMetaDataSource> SourcePriorityNamed
        {
            get => _sourcePriorityNamed ??= new(SourcePriority.Select(source => new NamedMetaDataSource(source)));
            set => SourcePriority = value.Select(namedSource => namedSource.Source).ToList();
        }

        /// <summary>
        /// Ordered List of sources that can set this prop, used for logic
        /// </summary>
        public List<MetaDataSource> SourcePriority { get; set; } = new();

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
            foreach (var source in DefaultSourcePriority)
            {
                SourcePriority.AddIfMissing(source);
            }

            // If a possible source was removed (due to a specific metadata fetch breaking
            // or due to an api change or something), remove it from the sources
            SourcePriority.RemoveAll(source => !DefaultSourcePriority.Contains(source));
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
    }
}
