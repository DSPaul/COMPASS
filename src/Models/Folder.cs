using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace COMPASS.Models
{
    public class Folder : ObservableObject, IHasChildren<Folder>
    {
        public Folder(string path)
        {
            FullPath = path;
        }

        public string FullPath { get; set; }

        public string Name => Path.GetFileName(FullPath);

        private ObservableCollection<Folder> _subFolders;
        public ObservableCollection<Folder> SubFolders
        {
            get => _subFolders ??= new(FindSubFolders());
            set => SetProperty(ref _subFolders, value);
        }

        public ObservableCollection<Folder> Children
        {
            get => SubFolders;
            set => SubFolders = value;
        }

        private IEnumerable<Folder> FindSubFolders()
        {
            var directories = Directory.GetDirectories(FullPath);
            return directories.Select(dir => new Folder(dir));
        }
    }
}
