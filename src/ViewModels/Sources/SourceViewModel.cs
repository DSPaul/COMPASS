using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Models;
using COMPASS.Models.XmlDtos;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public abstract class SourceViewModel : ObservableObject
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
