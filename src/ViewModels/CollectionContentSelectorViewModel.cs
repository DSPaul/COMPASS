using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace COMPASS.ViewModels
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
                CompleteCollection.Save();
            }
            else
            {
                CompleteCollection.Load(MakeStartupCollection: false);
            }

            //Checks which steps need to be included in wizard
            HasCodices = CompleteCollection.AllCodices.Any();
            HasTags = CompleteCollection.AllTags.Any();
            HasSettings = CompleteCollection.Info.ContainsSettings();
            UpdateSteps();

            //Put Tags in Checkable Wrapper
            TagsSelectorVM = new(completeCollection);

            //Put codices in dictionary so they can be labeled true/false for import
            SelectableCodices = CompleteCollection.AllCodices.Select(codex => new SelectableCodex(codex)).ToList();

            //prep settings data for selection
            AutoImportFolders = CompleteCollection.Info.AutoImportFolders.Select(folder => new SelectableWithPathHelper(folder.FullPath)).ToList();
            BanishedPaths = CompleteCollection.Info.BanishedPaths.Select(path => new SelectableWithPathHelper(path)).ToList();
            FileTypePrefs = CompleteCollection.Info.FiletypePreferences
                                                            .Select(x => new ObservableKeyValuePair<string, bool>(x))
                                                            .OrderByDescending(x => x.Value)
                                                            .ToList();
            FolderTagLinks = CompleteCollection.Info.FolderTagPairs
                .Select(link => new SelectableFolderTagLink(link.Folder, link.Tag!, CheckableTreeNode<Tag>.GetCheckedItems(SelectableTags).Flatten()))
                .ToList();
        }

        public const string TagsStep = "Tags";
        public const string ItemsStep = "Items";
        public const string SettingsStep = "Settings";

        /// <summary>
        /// Complete collections whose content will be subselected
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
        /// Indicates that only tags that are persent on codices should be imported
        /// </summary>
        public bool OnlyTagsOnCodices { get; set; } = false;

        // CODICES STEP
        public List<SelectableCodex> SelectableCodices { get; set; }

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
        public List<SelectableFolderTagLink> FolderTagLinks { get; init; }

        public CollectionViewSource FolderTagLinksVS
        {
            get
            {
                CollectionViewSource temp = new()
                {
                    Source = FolderTagLinks,
                };
                temp.SortDescriptions.Add(new SortDescription("Folder", ListSortDirection.Ascending));
                return temp;
            }
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

        public class SelectableFolderTagLink : SelectableWithPathHelper
        {
            public SelectableFolderTagLink(string path, Tag t, IEnumerable<Tag> existingTags) : base(path)
            {
                Tag = t;
                _existingTags = existingTags;
            }
            private IEnumerable<Tag> _existingTags;
            public Tag Tag { get; }
            public bool TagExists => _existingTags.Contains(Tag);
        }

        public class SelectableCodex : SelectableWithPathHelper
        {
            public SelectableCodex(Codex codex) : base(codex.Path)
            {
                Codex = codex;
            }
            public Codex Codex { get; }

            private RelayCommand<IList>? _itemCheckedCommand;
            public RelayCommand<IList> ItemCheckedCommand => _itemCheckedCommand ??= new((items) =>
                items?.Cast<SelectableCodex>()
                     .ToList()
                     .ForEach(c => c.Selected = Selected));
        }
        #endregion

        public void ApplySelectedTags()
        {
            if (!HasTags) return;

            if (OnlyTagsOnCodices) //indicates that we should keep all the tags that occur on the chosen codices
            {
                var assignedTags = CuratedCollection.AllCodices.SelectMany(c => c.Tags).Distinct().ToList(); //get all the tags that are assigned to a codex
                //delesect all tags
                TagsSelectorVM.SelectedTagCollection!.TagsRoot.IsChecked = false;
                var allselectableTags = SelectableTags.Flatten().ToList();
                foreach (var tag in assignedTags)
                {
                    allselectableTags.Single(st => st.Item == tag).IsChecked = true;
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

            CuratedCollection.Info.FolderTagPairs.Clear();
            if (SelectFolderTagLinks)
            {
                CuratedCollection.Info.FolderTagPairs = new(FolderTagLinks.Where(linkHelper => linkHelper.Selected && linkHelper.TagExists)
                                                                                   .Select(linkHelper => new FolderTagPair(linkHelper.Path, linkHelper.Tag)));
            }
        }

        /// <summary>
        /// Builds the curated collection based on the selection
        /// </summary>
        public override Task Finish()
        {
            //order is important!
            ApplySelectedCodices(); //first codices, makes copies, so further operations don't modify the existing ones
            ApplySelectedTags();
            ApplySelectedPreferences();
            return Task.CompletedTask;
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

            RaisePropertyChanged(nameof(Steps));
        }
    }
}
