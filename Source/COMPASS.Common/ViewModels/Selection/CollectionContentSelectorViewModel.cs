using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Attributes;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;
using System.Collections;
using System.ComponentModel;

namespace COMPASS.Common.ViewModels.Selection
{
    /// <summary>
    /// Class with logic to select only a subset of the content in a collection for import and export purposes
    /// </summary>
    public class CollectionContentSelectorViewModel : WizardViewModel, IDisposable
    {
        public CollectionContentSelectorViewModel(CodexCollection collection)
        {
            CompleteCollection = collection;

            //Checks which steps need to be included in wizard
            HasCodices = CompleteCollection.AllCodices.Any();
            HasTags = CompleteCollection.AllTags.Any();
            HasSettings = CompleteCollection.Info.ContainsSettings();
            UpdateSteps();

            //The create a temporary vm to be used in the UI
            _createdCollectionVm = new CodexCollectionVM(collection.Name, collection, StorageStrategy.Memory);
            
            //Put Tags in Checkable Wrapper
            TagsSelectorVM = new(_createdCollectionVm);

            //Put codices in dictionary so they can be labeled true/false for import
            SelectableCodices = CompleteCollection.AllCodices.Select(codex => new SelectableCodex(codex, this)).ToList();

            //prep settings data for selection
            AutoImportFolders = CompleteCollection.Info.AutoImportFolders.Select(folder => new SelectableWithPathHelper(folder.FullPath)).ToList();
            BanishedPaths = CompleteCollection.Info.BanishedPaths.Select(path => new SelectableWithPathHelper(path)).ToList();
            FileTypePrefs = CompleteCollection.Info.FiletypePreferences
                .Select(x => new ObservableKeyValuePair<string, bool>(x))
                .OrderByDescending(x => x.Value)
                .ToList();

            PersonalDataSelectors = typeof(Codex)
                .GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(PersonalDataAttribute)))
                .Select(p => new PersonalPropertySelectorViewModel(p.Name, p.GetCustomAttribute<PersonalDataAttribute>()!.DisplayName))
                .ToList();

            foreach (var selector in PersonalDataSelectors)
            {
                selector.PropertyChanged += OnPersonalDataSelectorPropertyChanged;
            }
        }
        
        public override string WindowTitle { get; } = "Choose content";

        public static readonly WizardStepViewModel TagsStep = new("Select Tags", "SelectTags");
        public static readonly WizardStepViewModel ItemsStep = new("Select Items", "SelectItems");
        public static readonly WizardStepViewModel SettingsStep = new("Select Settings", "SelectSettings");

        private readonly CodexCollectionVM _createdCollectionVm;

        /// <summary>
        /// Complete collections whose content will be sub selected
        /// </summary>
        public CodexCollection CompleteCollection { get; }

        /// <summary>
        /// Curated collection that contains only the selected items 
        /// </summary>
        public CodexCollection CuratedCollection => field ??= new($"{CompleteCollection.Name}_Curated");

        public bool HasTags { get; set; }
        public bool HasCodices { get; set; }
        public bool HasSettings { get; set; }

        //TAGS STEP
        public TagsSelectorViewModel TagsSelectorVM { get; set; }

        public IEnumerable<CheckableTreeNode<TagViewModel>> SelectableTags =>
            TagsSelectorVM.SelectedTagCollection?.TagsRoot.Children ?? Enumerable.Empty<CheckableTreeNode<TagViewModel>>();

        /// <summary>
        /// Indicates that only tags that are present on codices should be imported
        /// </summary>
        public bool OnlyTagsOnCodices { get; set; } = false;

        // CODICES STEP
        public List<SelectableCodex> SelectableCodices { get; set; }
        public int SelectedCodicesCount => SelectableCodices.Count(s => s.Selected);
        public void RaiseSelectedCodicesCountChanged() => OnPropertyChanged(nameof(SelectedCodicesCount));

        public List<PersonalPropertySelectorViewModel> PersonalDataSelectors { get; }

        public bool RemoveAllPersonalData
        {
            get => PersonalDataSelectors.All(s => s.Selected);
            set
            {
                foreach (var s in PersonalDataSelectors) s.Selected = value;
                OnPropertyChanged();
            }
        }

        //SETTINGS STEP

        //Auto Import Folders

        public bool SelectAutoImportFolders
        {
            get;
            set => SetProperty(ref field, value);
        }

        public List<SelectableWithPathHelper> AutoImportFolders { get; init; }

        //Banished paths

        public bool SelectBanishedFiles
        {
            get;
            set => SetProperty(ref field, value);
        }

        public List<SelectableWithPathHelper> BanishedPaths { get; init; }

        //File type preferences

        public bool SelectFileTypePrefs
        {
            get;
            set => SetProperty(ref field, value);
        }

        public List<ObservableKeyValuePair<string, bool>> FileTypePrefs { get; init; }

        //Tag-Folder links

        public bool SelectFolderTagLinks
        {
            get;
            set => SetProperty(ref field, value);
        }

        #region Helper classes

        public class SelectableWithPathHelper : ObservableObject
        {
            public SelectableWithPathHelper(string path)
            {
                Path = path;
                Selected = PathExits;
            }

            public bool Selected
            {
                get;
                set => SetProperty(ref field, value);
            }

            public string Path { get; set; }

            public bool PathExits => !System.IO.Path.IsPathFullyQualified(Path) || System.IO.Path.Exists(Path);
        }

        public class SelectableCodex : SelectableWithPathHelper
        {
            private CollectionContentSelectorViewModel _vm;

            public SelectableCodex(Codex codex, CollectionContentSelectorViewModel vm) : base(codex.Sources.Path)
            {
                Codex = codex;
                _vm = vm;
            }

            public Codex Codex { get; }

            public RelayCommand<IList> ItemCheckedCommand => field ??= new((items) =>
            {
                items?.Cast<SelectableCodex>()
                    .ToList()
                    .ForEach(c => c.Selected = Selected);
                _vm.RaiseSelectedCodicesCountChanged();
            });
        }

        #endregion

        public void ApplySelectedTags()
        {
            if (!HasTags) return;

            if (OnlyTagsOnCodices) //indicates that we should keep all the tags that occur on the chosen codices
            {
                var assignedTags = CuratedCollection.AllCodices.SelectMany(c => c.Tags).Distinct().ToList(); //get all the tags that are assigned to a codex
                //deselect all tags
                TagsSelectorVM.SelectedTagCollection!.TagsRoot.IsChecked = false;
                var allSelectableTags = SelectableTags.Flatten().ToList();
                foreach (var tag in assignedTags)
                {
                    allSelectableTags.Single(st => st.Item.Id == tag.Id).IsChecked = true;
                }

                CuratedCollection.RootTags = CheckableTreeNode.GetCheckedModels<TagViewModel, Tag>(SelectableTags).ToList();
            }
            else //otherwise use the users choice
            {
                CuratedCollection.RootTags = CheckableTreeNode.GetCheckedModels<TagViewModel, Tag>(SelectableTags).ToList();

                //Remove the tags that didn't make it from codices
                var removedTags = CompleteCollection.AllTags.Except(CuratedCollection.RootTags.Flatten()).ToList();

                foreach (Tag t in removedTags)
                {
                    CuratedCollection.AllTags.Remove(t);
                    foreach (var codex in CuratedCollection.AllCodices)
                    {
                        codex.Tags.Remove(t);
                    }
                }
            }
        }

        public void ApplySelectedCodices()
        {
            CuratedCollection.AllCodices.ReplaceRange(SelectableCodices.Where(x => x.Selected)
                .Select(x => x.Codex.Clone())); //clone codices to not modify the existing ones

            var propertiesToReset = PersonalDataSelectors.Where(pd => pd.Selected).Select(pd => pd.PropertyName);
            foreach (var prop in propertiesToReset)
            {
                foreach (var codex in CuratedCollection.AllCodices)
                {
                    codex.ResetPersonalProperty(prop);
                }
            }
        }

        public void ApplySelectedPreferences()
        {
            List<string> selectedFolderPaths = AutoImportFolders.Where(x => x.Selected).Select(x => x.Path).ToList();
            List<Folder> selectedFolders = CompleteCollection.Info.AutoImportFolders.Where(f => selectedFolderPaths.Contains(f.FullPath)).ToList();
            CuratedCollection.Info.AutoImportFolders.Clear();
            if (SelectAutoImportFolders)
            {
                CuratedCollection.Info.AutoImportFolders.ReplaceRange(selectedFolders);
            }

            CuratedCollection.Info.BanishedPaths.Clear();
            if (SelectBanishedFiles)
            {
                CuratedCollection.Info.BanishedPaths.ReplaceRange(BanishedPaths.Where(x => x.Selected).Select(x => x.Path));
            }

            CuratedCollection.Info.FiletypePreferences.Clear();
            if (SelectFileTypePrefs)
            {
                //for file types, select all or nothing because checking whether to select a checkbox becomes ridiculous
                CuratedCollection.Info.FiletypePreferences = CompleteCollection.Info.FiletypePreferences;
            }
        }

        /// <summary>
        /// Builds the curated collection based on the selection
        /// </summary>
        public void ApplyAllSelections()
        {
            //order is important!
            ApplySelectedCodices(); //first codices, makes copies, so further operations don't modify the existing ones
            ApplySelectedTags();
            ApplySelectedPreferences();
        }

        public void UpdateSteps()
        {
            //Checks which steps need to be included in wizard
            Steps.Clear();

            if (HasTags)
            {
                Steps.Add(TagsStep);
            }

            if (HasCodices)
            {
                Steps.Add(ItemsStep);
            }

            if (HasSettings)
            {
                Steps.Add(SettingsStep);
            }
        }
        
        private void OnPersonalDataSelectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PersonalPropertySelectorViewModel.Selected))
            {
                OnPropertyChanged(nameof(RemoveAllPersonalData));
            }
        }

        public void Dispose()
        {
            _createdCollectionVm.Dispose();
            foreach (var selector in PersonalDataSelectors)
            {
                selector.PropertyChanged -= OnPersonalDataSelectorPropertyChanged;
            }
        }
    }
}

