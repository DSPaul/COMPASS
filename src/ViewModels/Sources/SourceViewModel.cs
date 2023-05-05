using COMPASS.Models;
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

        public static SourceViewModel GetSourceVM(MetaDataSource source) => source switch
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

        public abstract bool IsValidSource(Codex codex);

        public abstract Task<Codex> GetMetaData(Codex codex);

        public abstract Task<bool> FetchCover(Codex codex);
        #endregion
    }
}
