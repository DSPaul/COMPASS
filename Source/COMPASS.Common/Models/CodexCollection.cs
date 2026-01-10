using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Models;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Models
{
    public class CodexCollection : ObservableObject
    {
        private IUserFilesStorageService _userFilesStorageService => field ??= ServiceResolver.Resolve<IUserFilesStorageService>();
        private IThumbnailStorageService _thumbnailStorageService => field ??=  ServiceResolver.Resolve<IThumbnailStorageService>();
        
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

        /// <summary>
        /// Will merge all the data from toMergeFrom into this collection
        /// </summary>
        /// <param name="toMergeFrom"></param>
        /// <param name="separateTags"> if true, all new tags will be put under a group with the name of the collection they came from</param>
        public void MergeWith(CodexCollection toMergeFrom, bool separateTags = false)
        {
            //Merge Tags
            if (separateTags)
            {
                var rootTag = new Tag(toMergeFrom.AllTags)
                {
                    IsGroup = true,
                    Name = toMergeFrom.Name.Trim('_'),
                    Children = new(toMergeFrom.RootTags)
                };
                toMergeFrom.RootTags = [rootTag];
            }
            AddTags(toMergeFrom.RootTags);

            //merge codices
            ImportCodicesFrom(toMergeFrom);

            //merge info
            Info.MergeWith(toMergeFrom.Info);
        }

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
            }
            RootTags.AddRange(tagsList);
            
            TagsChanged();
        }

        private void ImportCodicesFrom(CodexCollection source)
        {
            //if import includes files, make sure directory exists to copy files into
            bool canImportFiles = false;
            if (_userFilesStorageService.HasUserFiles(source))
            {
                canImportFiles = _userFilesStorageService.EnsureDirectoryExists(this);
                if (!canImportFiles)
                {
                    //TODO add a notification or similar that files will not be imported
                }
            }
            
            foreach (var codex in source.AllCodices)
            {
                //Give it a new id that is unique to this collection
                codex.Id = Utils.GetAvailableId(AllCodices);

                //Move thumbnail and cover
                _thumbnailStorageService.MoveCodexDataToCollection(codex, this);

                //move user files included in import
                if (canImportFiles)
                {
                    _userFilesStorageService.MoveCodexDataToCollection(codex, this, source, copy: true);
                }
                AllCodices.Add(codex);
            }
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

        public void DeleteTag(Tag toDelete)
        {
            //Remove from all codices
            foreach (var codex in AllCodices)
            {
                codex.Tags.Remove(toDelete);
            }
            
            //Recursive loop to delete all children
            if (toDelete.Children.Count > 0)
            {
                DeleteTag(toDelete.Children[0]);
                DeleteTag(toDelete);
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
