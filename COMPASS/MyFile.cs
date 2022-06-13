using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
namespace COMPASS
{
    public class MyFile
    {
        public MyFile()
        {
            this.Tags = new ObservableCollection<Tag>();
            int tempID = 0;
            while(Data.AllFiles.Any(f => f.ID == tempID))
            {
                tempID++;
            }
            ID = tempID;
            CoverArt = (Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\CoverArt\" + ID.ToString() + ".png");
        }

        public void Copy(MyFile f)
        {
            Title = f.Title;
            Path = f.Path;
            Author = f.Author;
            Publisher = f.Publisher;
            Version = f.Version;
            Source = f.Source;
            ID = f.ID;
            CoverArt = f.CoverArt;
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
        public string Source { get; set; }
        public int ID { get; set; }
        public string CoverArt { get; set; }

        public ObservableCollection<Tag> Tags { get; set; } 
    }
}

