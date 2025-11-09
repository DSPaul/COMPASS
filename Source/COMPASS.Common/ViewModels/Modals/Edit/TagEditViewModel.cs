using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Services.FileSystem;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools;
using Notification = COMPASS.Common.Models.Notification;

namespace COMPASS.Common.ViewModels.Modals.Edit
{
    public class TagEditViewModel : EditViewModelBase<Tag>
    {
        public TagEditViewModel(Tag sourceTag, bool createNew) : base(sourceTag, createNew)
        {
            _possibleParents = GetPossibleParents();
        }

        #region Properties
        
        private ObservableCollection<TreeNode<Tag>> _possibleParents;
        public ObservableCollection<TreeNode<Tag>> PossibleParents
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

        public bool HasPossibleParents => PossibleParents.Any(node => node.Item != _source);

        public TreeNode<Tag>? SelectedParent
        {
            get => PossibleParents.Flatten().FirstOrDefault(node => node.Item == WorkingCopy.Parent);
            set => WorkingCopy.Parent = value?.Item;
        }

        #endregion

        #region Methods and Commands

        //TODO move this to a Tag VM because it doesn't work here
        protected override void CustomValidate(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                ValidateName();
                return;
            }
            
            switch (propertyName)
            {
                case nameof(Tag.Name):
                    ValidateName();
                    break;
            }
        }

        private void ValidateName()
        {
            if (string.IsNullOrEmpty(WorkingCopy.Name))
            {
                AddError(nameof(WorkingCopy.Name), "Name is required.");
            }
            
            if (ActiveCollection.AllTags.Without(_source).Any(tag => tag.LongName == WorkingCopy.LongName))
            {
                AddError(nameof(WorkingCopy.Name), "Name must be unique within its parent.");
            }
        }

        protected override void HandleCreateNew(Tag newTag)
        {
            newTag.Id = Utils.GetAvailableId(ActiveCollection.AllTags);
            ActiveCollection.AllTags.Add(newTag);
                
            if (newTag.Parent == null)
            {
                ActiveCollection.RootTags.Add(newTag);
            }
            else
            {
                newTag.Parent.Children.Add(newTag);
            }
        }

        protected override void BeforeApply(Tag source, Tag proposal)
        {
            //if parent didn't change, no changes needed
            if (_source.Parent == WorkingCopy.Parent) return;
            
            //if the parent has changed, break the link with old parent
            IList<Tag> siblings = _source.Parent == null ? ActiveCollection.RootTags : _source.Parent.Children;
            siblings.Remove(_source);
        }

        protected override void OnApplied(Tag source)
        {
            //ensure parent-child link is bidirectional
            IList<Tag> siblings = _source.Parent == null ? ActiveCollection.RootTags : _source.Parent.Children;
            siblings.AddIfMissing(_source);
        }

        protected override void Clear()
        {
            base.Clear();

            //reset parents as new tag might have just been added
            PossibleParents = GetPossibleParents();
        }

        private ObservableCollection<TreeNode<Tag>> GetPossibleParents()
        {
            var collection = ActiveCollection.RootTags.Select(tag => new TreeNode<Tag>(tag)).ToList();

            foreach (TreeNode<Tag> node in collection.Flatten())
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
            
            var relevantCodices = ActiveCollection.AllCodices
                .Where(codex => codex.Sources.HasOfflineSource() &&
                                codex.Tags.Contains(_source))
                .ToList();

            var splitFolders = relevantCodices.Select(codex => codex.Sources.Path)
                                              .SelectMany(path => path.Split("\\"))
                                              .ToHashSet();

            foreach (string folder in splitFolders)
            {
                var codicesInFolder = ActiveCollection.AllCodices
                    .Where(codex => codex.Sources.HasOfflineSource())
                    .Where(codex => codex.Sources.Path.Contains(@"\" + folder + @"\"))
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
            List<Codex> matchingCodices = ActiveCollection.AllCodices
                .Where(codex => IOService.MatchesAnyGlob(codex.Sources.Path, globs) &&
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
            
            ActiveCollection.Save();
            TabsViewModel.GetInstance().ActiveTab?.TagsVM.UpdateTagsAsTreeNodes();
        }
        
        #endregion

        #region  IModalViewModel

        public override string WindowTitle => _createNew ? "Create new tag" : "Edit tag";

        #endregion
    }
}
