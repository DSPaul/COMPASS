using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
namespace COMPASS
{
    public class MyFile : ObservableObject
    {
        public MyFile()
        {
            Tags = new ObservableCollection<Tag>();

            //Give each tag an ID, "try" because can only be done when app has a Currentdata object
            try
            {
                int tempID = 0;
                while (UserSettings.CurrentData.AllFiles.Any(f => f.ID == tempID))
                {
                    tempID++;
                }
                ID = tempID;
            }

            catch { }
            CoverArt = (Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\CoverArt\" + ID.ToString() + ".png");
        }

        public void Copy(MyFile f)
        {
            Title = f.Title;
            Path = f.Path;
            Author = f.Author;
            Publisher = f.Publisher;
            Version = f.Version;
            SourceURL = f.SourceURL;
            ID = f.ID;
            CoverArt = f.CoverArt;
            Physically_Owned = f.Physically_Owned;
            Tags.Clear();
            foreach (Tag t in f.Tags)
            {
                Tags.Add(t);
            }
        }
        public string Path { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public string Version { get; set; }
        public string SourceURL { get; set; }
        public int ID { get; set; }
        public string CoverArt { get; set; }
        public bool Physically_Owned { get; set; }

        public ObservableCollection<Tag> Tags { get; set; } 
    }
}

