using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Tools;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Autofac;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models.Hierarchy;

namespace COMPASS.Common.ViewModels
{
    /// <summary>
    /// Class with logic to select only a subset of the content in a collection for import and export purposes
    /// </summary>
    public class CollectionContentSelectorViewModel : WizardViewModel
    {
        public CollectionContentSelectorViewModel(CodexCollection completeCollection)
        {
            CompleteCollection = completeCollection;

            if (MainViewModel.CollectionVM.CurrentCollection == completeCollection)
            {
                App.Container.Resolve<ICodexCollectionStorageService>().Save(CompleteCollection);
            }
            else
            {
                App.Container.Resolve<ICodexCollectionStorageService>().Load(CompleteCollection);
            }

            //Checks which steps need to be included in wizard
            HasCodices = CompleteCollection.AllCodices.Any();
            HasTags = CompleteCollection.AllTags.Any();
            HasSettings = CompleteCollection.Info.ContainsSettings();
            UpdateSteps();

            //Put Tags in Checkable Wrapper
            TagsSelectorVM = new(completeCollection);

            //Put codices in dictionary so they can be labeled true/false for import
            SelectableCodices = CompleteCollection.AllCodices.Select(codex => new SelectableCodex(codex, this)).ToList();

            //prep settings data for selection
            AutoImportFolders = CompleteCollection.Info.AutoImportFolders.Select(folder => new SelectableWithPathHelper(folder.FullPath)).ToList();
            BanishedPaths = CompleteCollection.Info.BanishedPaths.Select(path => new SelectableWithPathHelper(path)).ToList();
            FileTypePrefs = CompleteCollection.Info.FiletypePreferences
                                                            .Select(x => new ObservableKeyValuePair<string, bool>(x))
                                                            .OrderByDescending(x => x.Value)
                                                            .ToList();
        }

        public override string WindowTitle { get; } = "Choose content";
        
        public static readonly WizardStepViewModel TagsStep = new("Select Tags", "SelectTags");
        public static readonly WizardStepViewModel ItemsStep = new("Select Items", "SelectItems");
        public static readonly WizardStepViewModel SettingsStep = new("Select Settings", "SelectSettings");

        /// <summary>
        /// Complete collections whose content will be sub selected
        /// </summary>
        public CodexCollection CompleteCollection { get; set; }

        private CodexCollection? _curatedCollection;
        /// <summary>
        /// Curated collection that contains only the selected items 
        /// </summary>
        public CodexCollection CuratedCollection
        {
            get => _curatedCollection ??= new("__tmp_collection");
            set => _curatedCollection = value;
        }

        public bool HasTags { get; set; }
        public bool HasCodices { get; set; }
        public bool HasSettings { get; set; }

        //TAGS STEP
        public TagsSelectorViewModel TagsSelectorVM { get; set; }
        public IEnumerable<CheckableTreeNode<Tag>> SelectableTags => TagsSelectorVM.SelectedTagCollection?.TagsRoot.Children ?? Enumerable.Empty<CheckableTreeNode<Tag>>();

        /// <summary>
        /// Indicates that only tags that are present on codices should be imported
        /// </summary>
        public bool OnlyTagsOnCodices { get; set; } = false;

        // CODICES STEP
        public List<SelectableCodex> SelectableCodices { get; set; }
        public int SelectedCodicesCount => SelectableCodices.Count(s => s.Selected);
        public void RaiseSelectedCodicesCountChanged() => OnPropertyChanged(nameof(SelectedCodicesCount));

        public bool RemovePersonalData { get; set; } = true;

        //SETTINGS STEP

        //Auto Import Folders
        private bool _selectAutoImportFolders = false;
        public bool SelectAutoImportFolders
        {
            get => _selectAutoImportFolders;
            set => SetProperty(ref _selectAutoImportFolders, value);
        }
        public List<SelectableWithPathHelper> AutoImportFolders { get; init; }

        //Banished paths
        private bool _selectBanishedFiles = false;
        public bool SelectBanishedFiles
        {
            get => _selectBanishedFiles;
            set => SetProperty(ref _selectBanishedFiles, value);
        }
        public List<SelectableWithPathHelper> BanishedPaths { get; init; }

        //File type preferences
        private bool _selectFileTypePrefs = false;
        public bool SelectFileTypePrefs
        {
            get => _selectFileTypePrefs;
            set => SetProperty(ref _selectFileTypePrefs, value);
        }
        public List<ObservableKeyValuePair<string, bool>> FileTypePrefs { get; init; }

        //Tag-Folder links
        private bool _selectFolderTagLinks = false;
        public bool SelectFolderTagLinks
        {
            get => _selectFolderTagLinks;
            set => SetProperty(ref _selectFolderTagLinks, value);
        }

        #region Helper classes
        public class SelectableWithPathHelper : ObservableObject
        {
            public SelectableWithPathHelper(string path)
            {
                Path = path;
                Selected = PathExits;
            }

            private bool _selected;
            public bool Selected
            {
                get => _selected;
                set => SetProperty(ref _selected, value);
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

            private RelayCommand<IList>? _itemCheckedCommand;
            public RelayCommand<IList> ItemCheckedCommand => _itemCheckedCommand ??= new((items) =>
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
                    allSelectableTags.Single(st => st.Item.ID == tag.ID).IsChecked = true;
                }
                CuratedCollection.RootTags = CheckableTreeNode<Tag>.GetCheckedItems(SelectableTags).ToList();

            }
            else //otherwise use the users choice
            {
                CuratedCollection.RootTags = CheckableTreeNode<Tag>.GetCheckedItems(SelectableTags).ToList();

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
            CuratedCollection.AllCodices.Clear();
            CuratedCollection.AllCodices.AddRange(SelectableCodices.Where(x => x.Selected).Select(x => new Codex(x.Codex))); //make new codices to not modify the existing ones

            if (RemovePersonalData)
            {
                foreach (var codex in CuratedCollection.AllCodices)
                {
                    codex.ClearPersonalData();
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
                CuratedCollection.Info.AutoImportFolders = new(selectedFolders);
            }

            CuratedCollection.Info.BanishedPaths.Clear();
            if (SelectBanishedFiles)
            {
                CuratedCollection.Info.BanishedPaths = new(BanishedPaths.Where(x => x.Selected).Select(x => x.Path));
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
    }
}
