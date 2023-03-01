using COMPASS.Models;
using COMPASS.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace COMPASS.ViewModels.Sources
{
    abstract public class SourceViewModel : ObservableObject
    {
        public static SourceViewModel GetSource(ImportSource source) => source switch
        {
            ImportSource.File => new FileSourceViewModel(),
            ImportSource.Folder => new FolderSourceViewModel(),
            ImportSource.Manual => new ManualSourceViewModel(),
            ImportSource.ISBN => new ISBNSourceViewModel(),
            ImportSource.GmBinder => new GmBinderSourceViewModel(),
            ImportSource.Homebrewery => new HomebrewerySourceViewModel(),
            ImportSource.GoogleDrive => new GoogleDriveSourceViewModel(),
            ImportSource.GenericURL => new GenericOnlineSourceViewModel(),
            _ => throw new NotSupportedException()
        };

        #region Import Logic
        public abstract ImportSource Source { get; }
        public abstract void Import();

        public abstract Codex SetMetaData(Codex codex);

        public abstract bool FetchCover(Codex codex);
        #endregion

        #region Progress window stuff
        protected ProgressWindow GetProgressWindow() => new(this)
        {
            Owner = Application.Current.MainWindow
        };

        private float _progressPercentage;
        public float ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public int ProgressCounter { get; protected set; } = 0;
        public int ImportAmount { get; protected set; }

        private ObservableCollection<LogEntry> _importLog = new();
        public ObservableCollection<LogEntry> ImportLog
        {
            get => _importLog;
            set => SetProperty(ref _importLog, value);
        }

        public virtual string ProgressText => $"Import in Progress: {ProgressCounter + 1} / {ImportAmount}";

        public bool IsImporting { get; set; } = false; //true on initial import, false otherwise

        #endregion

        #region Asynchronous worker stuff

        protected BackgroundWorker worker;

        protected void InitWorker(DoWorkEventHandler workAction)
        {
            //Starts new threat (so program doesn't freeze while importing)
            worker = new() { WorkerReportsProgress = true };
            worker.DoWork += workAction;
            worker.ProgressChanged += ProgressChanged;
            worker.RunWorkerCompleted += WorkerComplete;
        }

        protected void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //calculate current percentage for progressbar
            ProgressPercentage = (int)((float)ProgressCounter / ImportAmount * 100);
            //update text
            RaisePropertyChanged(nameof(ProgressText));
            //write log entry if any
            if (e.UserState is LogEntry logEntry) ImportLog.Add(logEntry);
        }

        protected void WorkerComplete(object sender, EventArgs e)
        {
            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.FilterVM.ReFilter();
            ProgressCounter = 0;
        }
        #endregion
    }
}
