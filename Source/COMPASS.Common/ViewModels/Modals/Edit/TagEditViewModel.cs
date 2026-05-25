using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Hierarchy;
using System.Collections.ObjectModel;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools;
using Notification = COMPASS.Infra.Models.Notification;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Interfaces.Services;

namespace COMPASS.Common.ViewModels.Modals.Edit
{
    public class TagEditViewModel : EditViewModelBase<TagViewModel, Tag>
    {
        private readonly CodexCollectionVM _codexCollectionVm;
        
        public TagEditViewModel(Tag sourceTag, CodexCollectionVM collectionVm, bool createNew) : base(sourceTag, createNew, tag => new TagViewModel(tag, collectionVm))
        {
            _codexCollectionVm = collectionVm;
            _possibleParents = GetPossibleParents();
        }

        #region Properties
        
        private ObservableCollection<TreeNode<TagViewModel>> _possibleParents;
        public ObservableCollection<TreeNode<TagViewModel>> PossibleParents
        {
            get => _possibleParents;
            set
            {
                if (SetProperty(ref _possibleParents, value))
                {
                    OnPropertyChanged(nameof(HasPossibleParents));
                }
            }
        }

        public bool HasPossibleParents => PossibleParents.Any(node => node.Item.GetModel() != _source);

        public TreeNode<TagViewModel>? SelectedParent
        {
            get => PossibleParents.Flatten().FirstOrDefault(node => node.Item == WorkingCopy.Parent);
            set => WorkingCopy.Parent = value?.Item;
        }

        #endregion

        #region Methods and Commands

        protected override void HandleCreateNew(Tag newTag)
        {
            newTag.Id = Utils.GetAvailableId(_codexCollectionVm.Collection.AllTags);
            _codexCollectionVm.Collection.AllTags.Add(newTag);
                
            if (newTag.Parent == null)
            {
                _codexCollectionVm.Collection.RootTags.Add(newTag);
            }
            else
            {
                newTag.Parent.Children.Add(newTag);
            }
            
            _codexCollectionVm.Collection.TagsChanged();
        }

        protected override void BeforeApply(Tag source, Tag proposal)
        {
            //if parent didn't change, no changes needed
            if (_source.Parent == WorkingCopy.Parent?.GetModel()) return;
            
            //if the parent has changed, break the link with old parent
            IList<Tag> siblings = _source.Parent == null ? _codexCollectionVm.Collection.RootTags : _source.Parent.Children;
            siblings.Remove(_source);
        }

        protected override void OnApplied(Tag source)
        {
            //ensure parent-child link is bidirectional
            IList<Tag> siblings = _source.Parent == null ? _codexCollectionVm.Collection.RootTags : _source.Parent.Children;
            siblings.AddIfMissing(_source);
        }

        protected override void Clear()
        {
            base.Clear();

            //reset parents as new tag might have just been added
            PossibleParents = GetPossibleParents();
        }

        private ObservableCollection<TreeNode<TagViewModel>> GetPossibleParents()
        {
            var collection = _codexCollectionVm.Collection.RootTags
                .Select(_codexCollectionVm.GetTagVm)
                .Select(tagVm => new TreeNode<TagViewModel>(tagVm))
                .ToList();

            foreach (var node in collection.Flatten())
            {
                node.Expanded = node.Item.Children.Flatten().Contains(WorkingCopy.Parent); //expand all parents so that parent is visible
            }

            return new(collection);
        }

        private RelayCommand? _colorSameAsParentCommand;
        public RelayCommand ColorSameAsParentCommand => _colorSameAsParentCommand ??= new(SetColorSameAsParent);
        private void SetColorSameAsParent() => WorkingCopy.InternalBackgroundColor = null;

        private RelayCommand? _clearParentCommand;
        public RelayCommand ClearParentCommand => _clearParentCommand ??= new(ClearParent);
        private void ClearParent() => WorkingCopy.Parent = null;

        private RelayCommand? _detectLinksCommand;
        public RelayCommand DetectLinksCommand => _detectLinksCommand ??= new(DetectLinks, CanDetectLinks);

        private void DetectLinks()
        {
            //Can only detect links if tag exists
            if (_createNew) return;
            
            var relevantCodices = _codexCollectionVm.Collection.AllCodices
                .Where(codex => codex.Sources.HasOfflineSource() &&
                                codex.Tags.Contains(_source))
                .ToList();

            var splitFolders = relevantCodices.Select(codex => codex.Sources.Path)
                                              .SelectMany(path => path.Split(Path.DirectorySeparatorChar))
                                              .ToHashSet();

            foreach (string folder in splitFolders)
            {
                var codicesInFolder = _codexCollectionVm.Collection.AllCodices
                    .Where(codex => codex.Sources.HasOfflineSource())
                    .Where(codex => codex.Sources.Path.Contains(Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar))
                    .ToList();

                if (codicesInFolder.Count < 3) continue;  //Require at least 3 codices in same folder before we can speak of a pattern

                string glob = $"**/{folder}/**";

                if (codicesInFolder.All(codex => codex.Tags.Contains(_source)) &&
                    !WorkingCopy.CalculatedLinkedGlobs.Contains(glob))
                {
                    WorkingCopy.LinkedGlobs.AddIfMissing(glob);
                }
            }
        }
        private bool CanDetectLinks() => !_createNew;

        private AsyncRelayCommand? _applyLinksCommand;
        public AsyncRelayCommand ApplyLinksCommand => _applyLinksCommand ??= new(ApplyLinks, CanApplyChanges);
        private async Task ApplyLinks()
        {
            //Can only apply links if tag exists
            if (_createNew) return;
            
            var globs = WorkingCopy.LinkedGlobs.Concat(WorkingCopy.CalculatedLinkedGlobs).ToList();
            List<Codex> matchingCodices = _codexCollectionVm.Collection.AllCodices
                .Where(codex => PathUtils.MatchesAnyGlob(codex.Sources.Path, globs) &&
                                !codex.Tags.Contains(_source))
                .ToList();

            Notification notification;

            if (matchingCodices.Any())
            {
                notification = new(
                    $"{matchingCodices.Count} matching items found",
                    $"This tag will be added to {matchingCodices.Count} items")
                {
                    Details = string.Join('\n', matchingCodices.Select(codex => codex.Title)),
                    Actions = NotificationAction.Confirm | NotificationAction.Cancel,
                };
            }
            else
            {
                notification = new(
                    $"No new matching items found",
                    $"Either no matches were found or all matching items already contain this tag.");
            }

            await ServiceResolver.Resolve<INotificationService>().ShowDialog(notification);

            if (notification.Result == NotificationAction.Confirm)
            {
                foreach (Codex codex in matchingCodices)
                {
                    codex.Tags.Add(_source);
                }
            }
        }
        private bool CanApplyChanges() => !_createNew;

        #endregion

        #region IConfirmable
        
        protected override void Confirm()
        {
            base.Confirm();
            
            _codexCollectionVm.Collection.Save();
            TabsViewModel.GetInstance().ActiveTab?.TagsVM.UpdateTagsAsTreeNodes();
        }
        
        #endregion

        #region  IModalViewModel

        public override string WindowTitle => _createNew ? "Create new tag" : "Edit tag";

        #endregion
    }
}
