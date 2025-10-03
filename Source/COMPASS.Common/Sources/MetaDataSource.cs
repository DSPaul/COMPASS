using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Main;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public abstract class MetaDataSource
    {
        protected MetaDataSource(CodexCollection targetCollection)
        {
            TargetCollection = targetCollection;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="targetCollection"> Needed to have a list of tags that sources can choose from </param>
        /// <returns></returns>
        public static MetaDataSource? GetSource(MetaDataSourceType sourceType, CodexCollection targetCollection) => sourceType switch
        {
            MetaDataSourceType.File => new FileMetaDataSource(targetCollection),
            MetaDataSourceType.PDF => new PdfMetaDataSource(targetCollection),
            MetaDataSourceType.Image => new ImageMetaDataSource(targetCollection),
            MetaDataSourceType.ISBN => new ISBNMetaDataSource(targetCollection),
            MetaDataSourceType.GmBinder => new GmBinderMetaDataSource(targetCollection),
            MetaDataSourceType.Homebrewery => new HomebreweryMetaDataSource(targetCollection),
            MetaDataSourceType.GoogleDrive => new GoogleDriveMetaDataSource(targetCollection),
            MetaDataSourceType.GenericURL => new GenericOnlineMetaDataSource(targetCollection),
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
