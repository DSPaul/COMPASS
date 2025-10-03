using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using Notification = COMPASS.Common.Models.Notification;

namespace COMPASS.Common.ViewModels.Modals.Edit
{
    public class TagEditViewModel : ViewModelBase, IConfirmable, IModalViewModel
    {
        public TagEditViewModel(Tag? sourceTag, bool createNew) : base()
        {
            //if not creating a new tag, an existing tag should always be given
            if (!createNew)
            {
                ArgumentNullException.ThrowIfNull(sourceTag);
            }
            
            _sourceTag = sourceTag;
            CreateNewTag = createNew;

            _tempTag = sourceTag != null ? 
                new(sourceTag) : 
                new(ActiveCollection.AllTags);
            
            _tempTag.PropertyChanged += HandleTagPropertyChanged;

            _possibleParents = GetPossibleParents();
        }

        private void HandleTagPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tag.Name))
            {
                ConfirmCommand.NotifyCanExecuteChanged();
            }
        }

        #region Properties

        private readonly Tag? _sourceTag;
        public bool CreateNewTag { get; init; }

        //TempTag to work with
        private Tag _tempTag;
        public Tag TempTag
        {
            get => _tempTag;
            set
            {
                _tempTag.PropertyChanged -= HandleTagPropertyChanged;

                if (SetProperty(ref _tempTag, value))
                {
                    ConfirmCommand.NotifyCanExecuteChanged();
                }
                _tempTag.PropertyChanged += HandleTagPropertyChanged;
            }
        }

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

        public bool HasPossibleParents => PossibleParents.Any(node => node.Item != _sourceTag);

        public TreeNode<Tag>? SelectedParent
        {
            get => PossibleParents.Flatten().FirstOrDefault(node => node.Item == TempTag.Parent);
            set => TempTag.Parent = value?.Item;
        }

        #endregion

        #region Methods and Commands

        private void Clear()
        {
            TempTag = _sourceTag != null ? 
                new(_sourceTag) : 
                new(ActiveCollection.AllTags);

            //reset parents as new tag might have just been added
            PossibleParents = GetPossibleParents();
        }

        private ObservableCollection<TreeNode<Tag>> GetPossibleParents()
        {
            var collection = ActiveCollection.RootTags.Select(tag => new TreeNode<Tag>(tag)).ToList();

            foreach (TreeNode<Tag> node in collection.Flatten())
            {
                node.Expanded = node.Item.Children.Flatten().Contains(_tempTag.Parent); //expand all parents so that parent is visible
            }

            return new(collection);
        }

        private RelayCommand? _colorSameAsParentCommand;
        public RelayCommand ColorSameAsParentCommand => _colorSameAsParentCommand ??= new(SetColorSameAsParent);
        private void SetColorSameAsParent() => TempTag.InternalBackgroundColor = null;

        private RelayCommand? _clearParentCommand;
        public RelayCommand ClearParentCommand => _clearParentCommand ??= new(ClearParent);
        private void ClearParent() => TempTag.Parent = null;

        private RelayCommand? _detectLinksCommand;
        public RelayCommand DetectLinksCommand => _detectLinksCommand ??= new(DetectLinks, CanDetectLinks);

        private void DetectLinks()
        {
            //Can only detect links if tag exists
            if (_sourceTag == null || CreateNewTag) return;
            
            var relevantCodices = ActiveCollection.AllCodices
                .Where(codex => codex.Sources.HasOfflineSource() &&
                                codex.Tags.Contains(_sourceTag))
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

                if (codicesInFolder.All(codex => codex.Tags.Contains(_sourceTag)) &&
                    !TempTag.CalculatedLinkedGlobs.Contains(glob))
                {
                    TempTag.LinkedGlobs.AddIfMissing(glob);
                }
            }
        }

        private bool CanDetectLinks() => !CreateNewTag;

        private AsyncRelayCommand? _applyLinksCommand;
        public AsyncRelayCommand ApplyLinksCommand => _applyLinksCommand ??= new(ApplyLinks, CanApplyChanges);

        private async Task ApplyLinks()
        {
            //Can only apply links if tag exists
            if (_sourceTag == null || CreateNewTag) return;
            
            var globs = TempTag.LinkedGlobs.Concat(TempTag.CalculatedLinkedGlobs).ToList();
            List<Codex> matchingCodices = ActiveCollection.AllCodices
                .Where(codex => IOService.MatchesAnyGlob(codex.Sources.Path, globs) &&
                                !codex.Tags.Contains(_sourceTag))
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
                    codex.Tags.Add(_sourceTag);
                }
            }
        }

        private bool CanApplyChanges() => !CreateNewTag;

        #endregion

        #region IConfirmable

        private RelayCommand? _confirmCommand;
        public IRelayCommand ConfirmCommand => _confirmCommand ??= new(Confirm, CanConfirm);
        public void Confirm()
        {
            //Apply changes 
            if (CreateNewTag)
            {
                Tag newTag = new(TempTag)
                {
                    ID = Utils.GetAvailableID(ActiveCollection.AllTags)
                };
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
            else
            {
                //if not creating a new tag, an existing tag is always given
                Debug.Assert(_sourceTag != null);
                    
                Tag? oldParent = _sourceTag.Parent;
                _sourceTag.CopyFrom(TempTag);
                
                //handle parent changed
                if (oldParent != _sourceTag.Parent)
                {
                    // remove the link with old parent
                    if (oldParent == null)
                    {
                        ActiveCollection.RootTags.Remove(_sourceTag);
                    }
                    else
                    {
                        oldParent.Children.Remove(_sourceTag);
                    }
                    
                    //add the link to new parent
                    if (_sourceTag.Parent == null)
                    {
                        ActiveCollection.RootTags.Add(_sourceTag);
                    }
                    else
                    {
                        _sourceTag.Parent.Children.Add(_sourceTag);
                    }
                }
            }
            
            ActiveCollection.Save();

            TabsViewModel.GetInstance().ActiveTab?.TagsVM.UpdateTagsAsTreeNodes();

            //reset fields
            Clear();
            CloseAction();
        }
        public bool CanConfirm() => !string.IsNullOrWhiteSpace(TempTag.Name);

        private RelayCommand? _cancelCommand;
        public IRelayCommand CancelCommand => _cancelCommand ??= new(Cancel);
        public void Cancel()
        {
            Clear();
            CloseAction();
        }
        #endregion

        #region  IModalViewModel

        public string WindowTitle => CreateNewTag ? "Create new tag" : "Edit tag";
        public Action CloseAction { get; set; } = () => { };

        #endregion
    }
}
