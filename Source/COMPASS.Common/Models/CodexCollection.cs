using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;
using System.Collections.ObjectModel;

namespace COMPASS.Common.Models
{
    public class CodexCollection : ObservableObject
    {
        
        public CodexCollection(string identifier)
        {
            _name = identifier;
        }

        //To prevent saving a collection that hasn't loaded yet, which would wipe all your data
        public bool LoadedTags { get; set; }
        public bool LoadedCodices { get; set; }
        public bool LoadedInfo { get; set; }
        
        #region Properties
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ObservableCollection<Tag> AllTags { get; set; } = [];

        private List<Tag> _rootTags = [];
        public List<Tag> RootTags
        {
            get => _rootTags;
            set
            {
                SetProperty(ref _rootTags, value);
                foreach (Tag t in _rootTags)
                {
                    t.Parent = null;
                }
                TagsChanged();
            }
        }
        
        public RangeObservableCollection<Codex> AllCodices { get; } = [];

        public CollectionInfo Info { get; set; } = new();

        #endregion

        public void TagsChanged()
        {
            AllTags = new(RootTags.Flatten());
            OnPropertyChanged(nameof(RootTags));
        }

        public void AddTags(IEnumerable<Tag> tags)
        {
            // change ID's of Tags so there aren't any duplicates
            List<Tag> tagsList = tags.ToList();
            var tagsToImport = tagsList.Flatten();
            foreach (Tag tag in tagsToImport)
            {
                tag.Id = Utils.GetAvailableId(AllTags);
                AllTags.Add(tag);
            }
            RootTags.AddRange(tagsList);
            
            TagsChanged();
        }

        public void BanishCodices(IList<Codex> toBanish)
        {
            IEnumerable<string> toBanishPaths = toBanish.Select(codex => codex.Sources.Path);
            IEnumerable<string> toBanishURLs = toBanish.Select(codex => codex.Sources.SourceURL);
            IEnumerable<string> toBanishStrings = toBanishPaths
                .Concat(toBanishURLs)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToHashSet();

            Info.BanishedPaths.AddRange(toBanishStrings);
        }

        public async Task DeleteTag(Tag toDelete, INotificationService notificationService)
        {
            var inUseBy = AllCodices.Where(c => c.Tags.Contains(toDelete)).ToList();
            if (inUseBy.Any())
            {
                var codexNames = string.Join("\n - ", inUseBy.Select(c => c.Title));
                Notification confirm = Notification.AreYouSureNotification;
                confirm.Body = $"The tag '{toDelete.Name}' is currently in use by {inUseBy.Count} items.\n Are you sure you want to delete it?";
                confirm.Details = $"{toDelete.LongName} is currently assigned to:\n\n - {codexNames}";
                await notificationService.ShowDialog(confirm);
                
                if(confirm.Result != NotificationAction.Confirm)
                {
                    return;
                }
            }

            //Remove from all codices
            foreach (var codex in AllCodices)
            {
                codex.Tags.Remove(toDelete);
            }
            
            //Recursive loop to delete all children
            if (toDelete.Children.Count > 0)
            {
                await DeleteTag(toDelete.Children[0], notificationService);
                await DeleteTag(toDelete, notificationService);
            }

            //Remove the tag from all Tags
            AllTags.Remove(toDelete);

            //Remove the tags from parent's children list
            if (toDelete.Parent is null)
            {
                RootTags.Remove(toDelete);
            }
            else
            {
                toDelete.Parent.Children.Remove(toDelete);
            }
        }
    }
}
