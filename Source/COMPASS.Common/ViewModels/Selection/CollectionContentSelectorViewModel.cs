using System.Reflection;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Attributes;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;
using System.Collections;
using COMPASS.Infra.Models;

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
            AutoImportFoldersSelector = new(
                CompleteCollection.Info.AutoImportFolders.Select(folder => new SelectableWithPathHelper(folder.FullPath)));

            BanishedPathsSelector = new(
                CompleteCollection.Info.BanishedPaths.Select(path => new SelectableWithPathHelper(path)));

            FileTypePrefsSelector = new(
                CompleteCollection.Info.FiletypePreferences
                    .OrderByDescending(x => x.Value)
                    .Select(x => new ItemSelectorViewModel<ObservableKeyValuePair<string, bool>>(
                        new ObservableKeyValuePair<string, bool>(x), x.Key)));

            var personalProps = typeof(Codex)
                .GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(PersonalDataAttribute)));

            PersonalDataSelector = new(personalProps, p => p.GetCustomAttribute<PersonalDataAttribute>()!.DisplayName);
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

        public ItemsSelectorViewModel<PropertyInfo> PersonalDataSelector { get; }

        //SETTINGS STEP

        public ItemsSelectorViewModel<string> AutoImportFoldersSelector { get; init; }

        public ItemsSelectorViewModel<string> BanishedPathsSelector { get; init; }

        public ItemsSelectorViewModel<ObservableKeyValuePair<string, bool>> FileTypePrefsSelector { get; init; }

        #region Helper classes

        public class SelectableWithPathHelper : ItemSelectorViewModel<string>
        {
            public SelectableWithPathHelper(string path) : base(path)
            {
                Selected = PathExits;
            }

            public bool PathExits => !Path.IsPathFullyQualified(Item) || Path.Exists(Item);
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

            public RelayCommand<IList> ItemCheckedCommand => field ??= new(items =>
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

            var propertiesToReset = PersonalDataSelector.GetSelectedItems();
            foreach (var prop in propertiesToReset)
            {
                foreach (var codex in CuratedCollection.AllCodices)
                {
                    codex.ResetPersonalProperty(prop.Name);
                }
            }
        }

        public void ApplySelectedPreferences()
        {
            var selectedFolderPaths = AutoImportFoldersSelector.GetSelectedItems().ToList();
            List<Folder> selectedFolders = CompleteCollection.Info.AutoImportFolders
                .Where(f => selectedFolderPaths.Contains(f.FullPath)).ToList();

            CuratedCollection.Info.AutoImportFolders.Clear();
            CuratedCollection.Info.AutoImportFolders.ReplaceRange(selectedFolders);

            CuratedCollection.Info.BanishedPaths.Clear();
            CuratedCollection.Info.BanishedPaths.ReplaceRange(
                BanishedPathsSelector.GetSelectedItems());

            CuratedCollection.Info.FiletypePreferences.Clear();
            var selectedFileTypes = FileTypePrefsSelector.GetSelectedItems()
                .ToDictionary(x => x.Key, x => x.Value);

            if (selectedFileTypes.Any())
            {
                CuratedCollection.Info.FiletypePreferences = selectedFileTypes;
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
        
        public void Dispose()
        {
            _createdCollectionVm.Dispose();
            PersonalDataSelector.Dispose();
            AutoImportFoldersSelector.Dispose();
            BanishedPathsSelector.Dispose();
            FileTypePrefsSelector.Dispose();
        }
    }
}

