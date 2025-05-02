using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Common.Models
{
    public class SourceSet : ObservableObject
    {
        public SourceSet Copy() => new()
        {
            SourceURL = SourceURL,
            Path = Path,
            ISBN = ISBN
        };


        private string _sourceURL = "";
        public string SourceURL
        {
            get => _sourceURL;
            set
            {
                if (value.StartsWith("www."))
                {
                    value = @"https://" + value;
                }
                SetProperty(ref _sourceURL, value);
            }
        }

        private string _path = "";
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        private string _isbn = "";
        public string ISBN
        {
            get => _isbn;
            set => SetProperty(ref _isbn, value);
        }

        public string? FileType
        {
            get
            {
                if (HasOfflineSource())
                {
                    return System.IO.Path.GetExtension(Path);
                }

                else if (HasOnlineSource())
                {
                    // online sources can also point to file 
                    // either hosted on cloud service like Google Drive 
                    // or services like homebrewery are always .pdf
                    // skip this for now though
                    return "webpage";
                }

                else
                {
                    return null;
                }
            }
        }
        public string FileName => System.IO.Path.GetFileName(Path);

        public bool HasOfflineSource() => !String.IsNullOrWhiteSpace(Path);

        public bool HasOnlineSource() => !String.IsNullOrWhiteSpace(SourceURL);
    }
}
