using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.ViewModels.Sources
{
    abstract public class SourceViewModel : ObservableObject
    {
        public SourceViewModel() : this(MainViewModel.CollectionVM.CurrentCollection) { }
        public SourceViewModel(CodexCollection targetCollection)
        {
            TargetCollection = targetCollection;
        }

        public static SourceViewModel GetSource(ImportSource source) => GetSource(MainViewModel.CollectionVM.CurrentCollection, source);
        public static SourceViewModel GetSource(CodexCollection targetCollection, ImportSource source) => source switch
        {
            ImportSource.File => new FileSourceViewModel(targetCollection),
            ImportSource.Folder => new FolderSourceViewModel(targetCollection),
            ImportSource.Manual => new ManualSourceViewModel(targetCollection),
            ImportSource.ISBN => new ISBNSourceViewModel(targetCollection),
            ImportSource.GmBinder => new GmBinderSourceViewModel(targetCollection),
            ImportSource.Homebrewery => new HomebrewerySourceViewModel(targetCollection),
            ImportSource.GoogleDrive => new GoogleDriveSourceViewModel(targetCollection),
            ImportSource.GenericURL => new GenericOnlineSourceViewModel(targetCollection),
            _ => null
        };

        #region Import Logic

        public ProgressViewModel ProgressVM { get; set; } = new();

        protected CodexCollection TargetCollection;

        public abstract ImportSource Source { get; }
        public abstract void Import();

        protected async void ImportCodex(Codex newCodex)
        {
            //Start the Import
            LogEntry logEntry = new(LogEntry.MsgType.Info, $"Importing {Path.GetFileName(newCodex.HasOfflineSource() ? newCodex.Path : newCodex.SourceURL)}");
            ProgressChanged(logEntry);

            //Populate all the codex properties it can
            newCodex = await SetMetaData(newCodex);
            newCodex = SetTags(newCodex);

            //Fetch cover
            bool succes = await FetchCover(newCodex);
            if (!succes)
            {
                logEntry = new(LogEntry.MsgType.Warning, $"Failed to generate thumbnail for {Path.GetFileName(newCodex.Title)}");
                ProgressChanged(logEntry);
            }
            newCodex.RefreshThumbnail();

            //Complete Import
            string logMsg = $"Imported {newCodex.Title}";
            Logger.Info(logMsg);
            ProgressCounter++;
            ProgressChanged(new LogEntry(LogEntry.MsgType.Info, logMsg));
        }

        public abstract Task<Codex> SetMetaData(Codex codex);

        public abstract Codex SetTags(Codex codex);

        public abstract Task<bool> FetchCover(Codex codex);
        #endregion

        #region Progress window stuff
        protected ProgressWindow GetProgressWindow() => new(ProgressVM)
        {
            Owner = Application.Current.MainWindow
        };

        public int ProgressCounter { get; protected set; } = 0;
        public int ImportAmount { get; protected set; }

        public virtual string ProgressText => $"Import in Progress: {ProgressCounter + 1} / {ImportAmount}";

        public bool IsImporting { get; set; } = false; //true on initial import, false otherwise

        #endregion

        #region Asynchronous worker stuff

        protected void ProgressChanged(LogEntry logEntry = null) => Application.Current.Dispatcher.Invoke(() =>
            {
                //calculate current percentage for progressbar
                ProgressVM.SetPercentage(ProgressCounter, ImportAmount);
                ProgressVM.Text = ProgressText;
                //write log entry if any
                if (logEntry != null) ProgressVM.Log.Add(logEntry);
            });
        #endregion
    }
}
