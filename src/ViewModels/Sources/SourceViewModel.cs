using COMPASS.Models;
using System;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    abstract public class SourceViewModel : ObservableObject
    {
        public SourceViewModel() : this(MainViewModel.CollectionVM.CurrentCollection) { }
        public SourceViewModel(CodexCollection targetCollection)
        {
            TargetCollection = targetCollection;
        }

        public static SourceViewModel GetSource(MetaDataSource source) => source switch
        {
            MetaDataSource.File => new FileSourceViewModel(),
            MetaDataSource.PDF => new PdfSourceViewModel(),
            MetaDataSource.Image => new ImageSourceViewModel(),
            MetaDataSource.ISBN => new ISBNSourceViewModel(),
            MetaDataSource.GmBinder => new GmBinderSourceViewModel(),
            MetaDataSource.Homebrewery => new HomebrewerySourceViewModel(),
            MetaDataSource.GoogleDrive => new GoogleDriveSourceViewModel(),
            MetaDataSource.GenericURL => new GenericOnlineSourceViewModel(),
            _ => null
        };

        public static MetaDataSource? GetOnlineSource(string URL)
        {
            if (String.IsNullOrEmpty(URL)) return null;

            if (URL.Contains("dndbeyond.com"))
                return MetaDataSource.DnDBeyond;
            if (URL.Contains("gmbinder.com"))
                return MetaDataSource.GmBinder;
            if (URL.Contains("homebrewery.naturalcrit.com"))
                return MetaDataSource.Homebrewery;
            if (URL.Contains("drive.google.com"))
                return MetaDataSource.GoogleDrive;
            return MetaDataSource.GenericURL;
        }

        #region Import Logic

        protected ProgressViewModel ProgressVM => ProgressViewModel.GetInstance();

        protected CodexCollection TargetCollection;

        public abstract MetaDataSource Source { get; }

        public abstract Task<Codex> SetMetaData(Codex codex);

        public abstract Codex SetTags(Codex codex);

        public abstract Task<bool> FetchCover(Codex codex);
        #endregion
    }
}
