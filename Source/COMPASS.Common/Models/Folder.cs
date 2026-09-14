using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Interfaces;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Models
{
    public class Folder : ObservableObject, IHasChildren<Folder>
    {
        private readonly Func<Folder, IEnumerable<Folder>> _findSubfoldersOnDisk;

        public Folder(string path,  Func<Folder, IEnumerable<Folder>> findSubfoldersOnDisk)
        {
            FullPath = path.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar);
            _findSubfoldersOnDisk = findSubfoldersOnDisk;
        }

        public bool HasAllSubFolders => _explicitSubFolders == null;

        public string FullPath
        {
            get;
            set => SetProperty(ref field, value);
        }

        private RangeObservableCollection<Folder>? _explicitSubFolders;
        private RangeObservableCollection<Folder>? _allSubFolders;

        public RangeObservableCollection<Folder> SubFolders
        {
            get
            {
                if(_explicitSubFolders != null)
                {
                    return _explicitSubFolders;
                }

                // If no explicit subfolders are set, return all subfolders found on disk
                _allSubFolders ??= new(_findSubfoldersOnDisk(this));
                return _allSubFolders;
            }
        }

        public IReadOnlyList<Folder> AllSubFolders => _allSubFolders ??= new(_findSubfoldersOnDisk(this));

        public void SetExplicitSubfolders(IEnumerable<Folder> folders)
        {
            if(folders.Any(f => !PathUtils.IsPathInsideDirectory(f.FullPath, FullPath)))
            {
                throw new InvalidOperationException($"Cannot set explicit subfolders for {FullPath} because it contains a folder that is not a subfolder of this folder.");
            }

            _explicitSubFolders = [];
            _explicitSubFolders.ReplaceRange(folders);
            OnPropertyChanged(nameof(SubFolders));
        }

        public void ClearExplicitSubfolders()
        {
            _explicitSubFolders = null;
            OnPropertyChanged(nameof(SubFolders));
        }

        public void UpdateAllSubFolders()
        {
            //Invalidate the cache,
            //this means new instances of subfolders will be created,
            //always compare folders by fullpath for this reason
            _allSubFolders = null;
            OnPropertyChanged(nameof(SubFolders));
        }

        public string Name => Path.GetFileName(FullPath);
        
        //Proxy for Subfolder, to implement IHasChildren
        public RangeObservableCollection<Folder> Children => SubFolders;
    }

    [Factory]
    public class FolderFactory(ILogger logger, IIOService ioService)
    {
        public Folder Create(string path) => Create(path, new HashSet<string>(PathUtils.PathComparer));

        private Folder Create(string path, HashSet<string> ancestorIdentities)
        {
            if (!Path.Exists(path))
            {
                logger.Debug($"Folder not found on disk: '{path}'");
            }

            return new Folder(path, f => GetSubfoldersFromDisk(f, ancestorIdentities));
        }

        private IEnumerable<Folder> GetSubfoldersFromDisk(Folder f, ICollection<string> ancestorIdentities)
        {
            if (!Path.Exists(f.FullPath))
            {
                logger.Debug($"Folder not found on disk: '{f.FullPath}' while looking for subfolders");
                yield break;
            }

            //Make copy so each folder only has as list of direct parents, instead of sharing list with whole tree
            var ancestors = new HashSet<string>(ancestorIdentities, PathUtils.PathComparer)
            {
                PathUtils.GetDirectoryIdentity(f.FullPath)
            };

            foreach (string subFolder in ioService.TryGetDirectories(f.FullPath))
            {
                if (ancestors.Contains(PathUtils.GetDirectoryIdentity(subFolder)))
                {
                    logger.Warn($"Skipping '{subFolder}': filesystem cycle detected.");
                    continue;
                }
                yield return Create(subFolder, ancestors);
            }
        }
    }
}
