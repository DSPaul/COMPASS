using Autofac;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.Interfaces.Storage;

namespace COMPASS.Common.Models
{
    public class CodexCollection : ObservableObject
    {
        public CodexCollection(string collectionDirectory)
        {
            _name = collectionDirectory;
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

        public List<Tag> AllTags { get; set; } = [];

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

        private ObservableCollection<Codex> _allCodices = [];
        public ObservableCollection<Codex> AllCodices
        {
            get => _allCodices;
            set => SetProperty(ref _allCodices, value);
        }

        public CollectionInfo Info { get; set; } = new();

        #endregion

        /// <summary>
        /// Will merge all the data from toMergeFrom into this collection
        /// </summary>
        /// <param name="toMergeFrom"></param>
        /// <param name="separateTags"> if true, all new tags will be put under a group with the name of the collection they came from</param>
        public async Task MergeWith(CodexCollection toMergeFrom, bool separateTags = false)
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

            //save
            if (MainViewModel.CollectionVM.CurrentCollection == this)
            {
                await MainViewModel.CollectionVM.Refresh();
            }
            else
            {
                App.Container.Resolve<ICodexCollectionStorageService>().Save(this);
            }
        }

        public void TagsChanged()
        {
            AllTags = RootTags.Flatten().ToList();
        }
        
        public void RenameCollection(string newCollectionName)
        {
            string oldName = Name;
            Name = newCollectionName;
            
            App.Container.Resolve<ICodexCollectionStorageService>().OnCollectionRenamed(oldName, newCollectionName);
            App.Container.Resolve<IThumbnailStorageService>().OnCollectionRenamed(this);

            Logger.Info($"Renamed {oldName} to {newCollectionName}");
        }

        public void AddTags(IEnumerable<Tag> tags)
        {
            // change ID's of Tags so there aren't any duplicates
            List<Tag> tagsList = tags.ToList();
            var tagsToImport = tagsList.Flatten();
            foreach (Tag tag in tagsToImport)
            {
                tag.ID = Utils.GetAvailableID(AllTags);
                AllTags.Add(tag);
            }
            RootTags.AddRange(tagsList);
            MainViewModel.CollectionVM.TagsVM.UpdateTagsAsTreeNodes();
        }

        public void ImportCodicesFrom(CodexCollection source)
        {
            var userFilesStorageService = App.Container.Resolve<IUserFilesStorageService>();
            var thumbnailStorageService = App.Container.Resolve<IThumbnailStorageService>();
            
            //if import includes files, make sure directory exists to copy files into
            bool canImportFiles = false;
            if (userFilesStorageService.HasUserFiles(source))
            {
                canImportFiles = userFilesStorageService.EnsureDirectoryExists(this);
                if (!canImportFiles)
                {
                    //TODO add a notification or similar that files will not be imported
                }
            }
            
            foreach (var codex in source.AllCodices)
            {
                //Give it a new id that is unique to this collection
                codex.ID = Utils.GetAvailableID(AllCodices);

                //Move thumbnail and cover
                thumbnailStorageService.MoveCodexDataToCollection(codex, this);

                //move user files included in import
                if (canImportFiles)
                {
                    userFilesStorageService.MoveCodexDataToCollection(codex, this, source, copy: true);
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

            App.Container.Resolve<ICodexCollectionStorageService>().SaveTags(this);
        }
    }
}
