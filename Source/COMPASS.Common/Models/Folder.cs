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
        public Folder(string path, IEnumerable<Folder> allSubFolders)
        {
            FullPath = path.Trim(Path.DirectorySeparatorChar).Trim(Path.AltDirectorySeparatorChar);
            _allSubFolders = new RangeObservableCollection<Folder>(allSubFolders);
        }

        public bool HasAllSubFolders => _explicitSubFolders == null;

        public string FullPath
        {
            get;
            set => SetProperty(ref field, value);
        }

        private RangeObservableCollection<Folder>? _explicitSubFolders;
        private RangeObservableCollection<Folder> _allSubFolders;

        public RangeObservableCollection<Folder> SubFolders
        {
            get => _explicitSubFolders ?? _allSubFolders;
        }

        public IReadOnlyList<Folder> AllSubFolders => _allSubFolders;

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

        public void UpdateAllSubFolders(FolderFactory folderFactory)
        {
            _allSubFolders = folderFactory.Create(FullPath)._allSubFolders;
            OnPropertyChanged(nameof(SubFolders));
        }

        public string Name => Path.GetFileName(FullPath);
        
        //Proxy for Subfolder, to implement IHasChildren
        public RangeObservableCollection<Folder> Children => SubFolders;
    }

    [Factory]
    public class FolderFactory(IIOService ioService)
    {
        public Folder Create(string path)
        {
            var subFolders = ioService.TryGetDirectories(path);
            var folder = new Folder(path, subFolders.Select(Create));
            return folder;
        }
    }
}
