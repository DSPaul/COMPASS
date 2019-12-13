using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace COMPASS
{
    class Data
    {
        public static ObservableCollection<Tag> AllTags = new ObservableCollection<Tag>();
        public static ObservableCollection<Tag> RootTags = new ObservableCollection<Tag>();
        public static ObservableCollection<MyFile> AllFiles = new ObservableCollection<MyFile>();
        public static ObservableCollection<MyFile> TagFilteredFiles = new ObservableCollection<MyFile>(AllFiles);
        public static ObservableCollection<MyFile> SearchFilteredFiles = new ObservableCollection<MyFile>(AllFiles);
        public static ObservableCollection<MyFile> ActiveFiles = new ObservableCollection<MyFile>(AllFiles);
        public static ObservableCollection<Tag> ActiveTags = new ObservableCollection<Tag>();

        public static MyFile EditedFile = new MyFile();
        public static Tag EditedTag = new Tag();

        public static ObservableCollection<String> AuthorList = new ObservableCollection<string>();
        public static ObservableCollection<String> SourceList = new ObservableCollection<string>();
        public static ObservableCollection<String> PublisherList = new ObservableCollection<string>();

    }
}
