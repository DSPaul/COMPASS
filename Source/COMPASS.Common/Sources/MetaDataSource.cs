using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.ViewModels;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public abstract class MetaDataSource
    {
        protected MetaDataSource() : this(MainViewModel.CollectionVM.CurrentCollection) { }

        protected MetaDataSource(CodexCollection targetCollection)
        {
            TargetCollection = targetCollection;
        }

        public static MetaDataSource? GetSource(MetaDataSourceType sourceType) => sourceType switch
        {
            MetaDataSourceType.File => new FileMetaDataSource(),
            MetaDataSourceType.PDF => new PdfMetaDataSource(),
            MetaDataSourceType.Image => new ImageMetaDataSource(),
            MetaDataSourceType.ISBN => new ISBNMetaDataSource(),
            MetaDataSourceType.GmBinder => new GmBinderMetaDataSource(),
            MetaDataSourceType.Homebrewery => new HomebreweryMetaDataSource(),
            MetaDataSourceType.GoogleDrive => new GoogleDriveMetaDataSource(),
            MetaDataSourceType.GenericURL => new GenericOnlineMetaDataSource(),
            _ => null
        };

        #region Import Logic

        protected ProgressViewModel ProgressVM => ProgressViewModel.GetInstance();

        protected CodexCollection TargetCollection;

        public abstract MetaDataSourceType Type { get; }

        public abstract bool IsValidSource(SourceSet sources);

        public abstract Task<SourceMetaData> GetMetaData(SourceSet sources);

        public abstract Task<IMagickImage?> FetchCover(SourceSet sources);
        #endregion
    }
}
