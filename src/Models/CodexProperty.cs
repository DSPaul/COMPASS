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

        public CodexProperty(string label, Func<Codex, bool> isEmpty, Func<Codex, object> getProp, Action<Codex, Codex> setProp, List<NamedMetaDataSource> defaultSources)
        {
            Label = label;
            IsEmpty = isEmpty;
            GetProp = getProp;
            SetProp = setProp;
            DefaultSourcePriority = defaultSources;
        }

        public string Label { get; init; }

        private Func<Codex, bool> _isEmpty;
        [XmlIgnore]
        public Func<Codex, bool> IsEmpty
        {
            get => _isEmpty ??= Codex.Properties.First(prop => prop.Label == Label).IsEmpty;
            init => _isEmpty = value;
        }

        private Func<Codex, object> _getProp;
        [XmlIgnore]
        public Func<Codex, object> GetProp
        {
            get => _getProp ??= Codex.Properties.First(prop => prop.Label == Label).GetProp;
            init => _getProp = value;
        }

        private Action<Codex, Codex> _setProp;
        [XmlIgnore]
        public Action<Codex, Codex> SetProp
        {
            get => _setProp ??= Codex.Properties.First(prop => prop.Label == Label).SetProp;
            init => _setProp = value;
        }

        #region Import Sources
        private List<NamedMetaDataSource> _defaultSources;
        [XmlIgnore]
        protected List<NamedMetaDataSource> DefaultSourcePriority
        {
            get => _defaultSources ??= Codex.Properties.First(prop => prop.Label == Label).DefaultSourcePriority;
            init => _defaultSources = value;
        }

        /// <summary>
        /// Ordered List of sources that can set this prop, named for databinding
        /// </summary>
        private ObservableCollection<NamedMetaDataSource> _sources = new();
        public ObservableCollection<NamedMetaDataSource> SourcePriorityNamed
        {
            get => _sources;
            set => SetProperty(ref _sources, value);
        }

        /// <summary>
        /// Ordered List of sources that can set this prop, used for logic
        /// </summary>
        [XmlIgnore]
        public List<MetaDataSource> SourcePriority => SourcePriorityNamed.Select(namedSource => namedSource.Source).ToList();

        private MetaDataOverwriteMode _overwriteMode = MetaDataOverwriteMode.IfEmpty;
        public MetaDataOverwriteMode? OverwriteMode
        {
            get => _overwriteMode;
            set
            {
                if (value is not null)
                {
                    SetProperty<MetaDataOverwriteMode>(ref _overwriteMode, (MetaDataOverwriteMode)value);
                }
            }
        }

        public void UpdateSources()
        {
            // If a new possible source was not found in the save, add it
            foreach (var source in DefaultSourcePriority)
            {
                SourcePriorityNamed.AddIfMissing(source);
            }

            //if a possible source was removed (due to a specific metadata fetch breaking
            // due to an api change or sometinng), remove it from the sources
            List<NamedMetaDataSource> toRemove = new();
            foreach (var source in SourcePriorityNamed)
            {
                if (!DefaultSourcePriority.Contains(source)) toRemove.Add(source);
            }
            foreach (var source in toRemove)
            {
                SourcePriorityNamed.Remove(source);
            }
        }

        #endregion
    }
}
