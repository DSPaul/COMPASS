using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace COMPASS.Common.Models
{
    public class Folder : ObservableObject, IHasChildren<Folder>
    {
        //Parameterless ctor for xml deserialization
        public Folder() { }

        public Folder(string path)
        {
            _fullPath = path;
        }

        /// <summary>
        /// Indicates wheter or not this folder has all the existing subfolders, or only the ones specified in <see cref="SubFolders"/>
        /// If this is true, and a new subfolder was created, it will be added to the subfolders, if not, subfolders remains unchanged.
        /// </summary>
        public bool HasAllSubFolders { get; set; } = true;

        private string _fullPath = "";
        public string FullPath
        {
            get => _fullPath;
            set => SetProperty(ref _fullPath, value);
        }

        private ObservableCollection<Folder>? _subFolders;
        public ObservableCollection<Folder> SubFolders
        {
            get
            {
                AddNewSubFolders();
                return _subFolders ??= new(FindSubFolders());
            }

            set => SetProperty(ref _subFolders, value);
        }

        public string Name => Path.GetFileName(FullPath);

        [XmlIgnore]
        public ObservableCollection<Folder> Children
        {
            get => SubFolders;
            set => SubFolders = value;
        }

        private IEnumerable<Folder> FindSubFolders()
        {
            if (Directory.Exists(FullPath))
            {
                try
                {
                    var directories = Directory.GetDirectories(FullPath);
                    return directories.Select(dir => new Folder(dir));
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to get subfolders of {FullPath}", ex);
                }
            }

            return Enumerable.Empty<Folder>();
        }

        /// <summary>
        /// Looks for new subfolders and updates its subfolders if needed
        /// </summary>
        public void AddNewSubFolders()
        {
            if (HasAllSubFolders)
            {
                _subFolders = null;
            }
            else
            {
                foreach (Folder folder in _subFolders!)
                {
                    folder.AddNewSubFolders();
                }
            }
        }
    }
}
