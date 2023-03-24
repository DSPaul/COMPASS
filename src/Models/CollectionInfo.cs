using System.Collections.ObjectModel;

namespace COMPASS.Models
{
    /// <summary>
    /// Contains all the Info on a Collection That needs to be serialized
    /// </summary>
    public class CollectionInfo
    {
        //Folders to check for new files
        public ObservableCollection<string> AutoImportDirectories { get; set; } = new();
    }
}
