using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.XmlDtos;

namespace COMPASS.Common.ViewModels.Sources
{
    public abstract class SourceViewModel : ViewModelBase
    {
        protected SourceViewModel() : this(MainViewModel.CollectionVM.CurrentCollection) { }

        protected SourceViewModel(CodexCollection targetCollection)
        {
            TargetCollection = targetCollection;
        }

        public static SourceViewModel? GetSourceVM(MetaDataSource source) => source switch
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

        #region Import Logic

        protected ProgressViewModel ProgressVM => ProgressViewModel.GetInstance();

        protected CodexCollection TargetCollection;

        public abstract MetaDataSource Source { get; }

        public abstract bool IsValidSource(SourceSet sources);

        public abstract Task<CodexDto> GetMetaData(SourceSet sources);

        public abstract Task<bool> FetchCover(Codex codex);
        #endregion
    }
}
