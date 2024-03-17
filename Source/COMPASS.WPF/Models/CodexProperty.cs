using COMPASS.Tools;
using COMPASS.ViewModels.Sources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public class CodexProperty : ObservableObject
    {
        //Empty ctor for serialization
        public CodexProperty() { }

        public CodexProperty(string propName, Func<Codex, bool> isEmpty, Action<Codex, Codex> setProp, List<MetaDataSource> defaultSources, string? label = null)
        {
            Name = propName;
            Label = label ?? propName;
            IsEmpty = isEmpty;
            SetProp = setProp;
            DefaultSourcePriority = defaultSources;
        }

        public string Name { get; init; } = "";

        [XmlIgnore]
        public string Label { get; init; } = "";

        private Func<Codex, bool>? _isEmpty;
        [XmlIgnore]
        public Func<Codex, bool> IsEmpty
        {
            get => _isEmpty ??= Codex.Properties.First(prop => prop.Name == Name).IsEmpty;
            private init => _isEmpty = value;
        }

        private Func<Codex, object?>? _getProp;
        [XmlIgnore]
        public Func<Codex, object?> GetProp => _getProp ??= codex => codex.GetPropertyValue(Name);

        private Action<Codex, Codex>? _setProp;
        [XmlIgnore]
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
        [XmlIgnore]

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
        public MetaDataOverwriteMode? OverwriteMode
        {
            get => _overwriteMode;
            set
            {
                if (value is not null)
                {
                    SetProperty(ref _overwriteMode, (MetaDataOverwriteMode)value);
                }
            }
        }

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

        public void PrepareForSave() =>
            //Use order from NameMetaDataSources (which was reordered by user)
            SourcePriority = SourcePriorityNamed.Select(namedSource => namedSource.Source).ToList();

        #endregion
    }
}
