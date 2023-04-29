using COMPASS.Tools;
using COMPASS.ViewModels.Sources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public class MetaDataPreference : ObservableObject
    {
        //Empty ctor for serialization
        public MetaDataPreference() { }

        public MetaDataPreference(string label)
        {
            Label = label;
        }

        public string Label { get; init; }

        /// <summary>
        /// Ordered List of sources that is used for databinding
        /// </summary>
        private ObservableCollection<NamedImportSource> _sources = new();
        public ObservableCollection<NamedImportSource> SourcePriorityNamed
        {
            get => _sources;
            set => SetProperty(ref _sources, value);
        }

        /// <summary>
        /// Ordered List of sources that is used for logic
        /// </summary>
        [XmlIgnore]
        public List<ImportSource> SourcePriority =>
            SourcePriorityNamed.Select(namedSource => namedSource.Source).ToList();

        public MetaDataBehavior Behavior { get; set; } = MetaDataBehavior.IfEmpty;

        public void Init(List<NamedImportSource> PossibleSources)
        {
            // If a new possible source was not found in the save, add it
            foreach (var source in PossibleSources)
            {
                SourcePriorityNamed.AddIfMissing(source);
            }

            //if a possible source was removed (due to a specific metadata fetch breaking
            // due to an api change or sometinng), remove it from the sources
            List<NamedImportSource> toRemove = new();
            foreach (var source in SourcePriorityNamed)
            {
                if (!PossibleSources.Contains(source)) toRemove.Add(source);
            }
            foreach (var source in toRemove)
            {
                SourcePriorityNamed.Remove(source);
            }
        }
    }
}
