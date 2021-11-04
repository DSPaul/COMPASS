using COMPASS.Models;
using COMPASS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
namespace COMPASS.Models
{
    public class Codex : ObservableObject
    {
        public Codex()
        {
            Tags = new ObservableCollection<Tag>();
        }

        public Codex(CodexCollection cc)
        {
            Tags = new ObservableCollection<Tag>();
            ID = cc.GetAvailableID();
            CoverArt = CodexCollection.CollectionsPath + cc.Folder + @"\CoverArt\" + ID.ToString() + ".png";
        }

        public void Copy(Codex c)
        {
            Title = c.Title;
            Path = c.Path;
            Author = c.Author;
            Publisher = c.Publisher;
            Version = c.Version;
            SourceURL = c.SourceURL;
            ID = c.ID;
            CoverArt = c.CoverArt;
            Physically_Owned = c.Physically_Owned;
            Description = c.Description;
            ReleaseDate = c.ReleaseDate;
            Rating = c.Rating;
            PageCount = c.PageCount;
            Tags.Clear();
            foreach (Tag t in c.Tags)
            {
                Tags.Add(t);
            }
        }

        private string _Path;
        private string _Title;
        private string _Author;
        private string _Publisher;
        private string _Version;
        private string _SourceURL;
        private int _ID;
        private string _CoverArt;
        private bool _Physically_Owned;
        private string _Description;
        private DateTime? _ReleaseDate = null;
        private int _Rating;
        private int _PageCount;

        private ObservableCollection<Tag> _Tags;

        #region Getters and Setters
        public string Path
        {
            get { return _Path; }
            set { SetProperty(ref _Path, value); }
        }
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }
        public string Author
        {
            get { return _Author; }
            set { SetProperty(ref _Author, value); }
        }
        public string Publisher
        {
            get { return _Publisher; }
            set { SetProperty(ref _Publisher, value); }
        }
        public string Version
        {
            get { return _Version; }
            set { SetProperty(ref _Version, value); }
        }
        public string SourceURL
        {
            get { return _SourceURL; }
            set { SetProperty(ref _SourceURL, value); }
        }
        public int ID
        {
            get { return _ID; }
            set { SetProperty(ref _ID, value); }
        }
        public string CoverArt
        {
            get { return _CoverArt; }
            set { SetProperty(ref _CoverArt, value); }
        }
        public bool Physically_Owned
        {
            get { return _Physically_Owned; }
            set { SetProperty(ref _Physically_Owned, value); }
        }
        public ObservableCollection<Tag> Tags
        {
            get { return _Tags; }
            set { SetProperty(ref _Tags, value); }
        }

        public string Description
        {
            get { return _Description; }
            set { SetProperty(ref _Description, value); }
        }

        public DateTime? ReleaseDate
        {
            get { return _ReleaseDate; }
            set { SetProperty(ref _ReleaseDate, value); }
        }

        public int Rating
        {
            get { return _Rating; }
            set { SetProperty(ref _Rating, value); }
        }

        public int PageCount
        {
            get { return _PageCount; }
            set { SetProperty(ref _PageCount, value); }
        }
        #endregion 
    }
}

