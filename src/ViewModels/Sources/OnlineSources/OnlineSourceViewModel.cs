using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using HtmlAgilityPack;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.ViewModels.Sources
{
    public abstract class OnlineSourceViewModel : SourceViewModel
    {
        public OnlineSourceViewModel() : base() { }
        public OnlineSourceViewModel(CodexCollection targetCollection) : base(targetCollection) { }

        public static ImportSource? GetOnlineSource(string URL)
        {
            if (String.IsNullOrEmpty(URL)) return null;

            if (URL.Contains("dndbeyond.com"))
                return ImportSource.DnDBeyond;
            if (URL.Contains("gmbinder.com"))
                return ImportSource.GmBinder;
            if (URL.Contains("homebrewery.naturalcrit.com"))
                return ImportSource.Homebrewery;
            if (URL.Contains("drive.google.com"))
                return ImportSource.GoogleDrive;
            return ImportSource.GenericURL;
        }

        #region Import logic
        public override void Import()
        {
            IsImporting = true;
            importURLwindow = new(this);
            importURLwindow.Show();
        }

        public async void ImportURL()
        {
            Codex newCodex = new(MainViewModel.CollectionVM.CurrentCollection)
            {
                SourceURL = InputURL
            };

            // Steps 1 & 2: Load Source and Scrape metadata
            newCodex = await SetMetaData(newCodex);
            newCodex = SetTags(newCodex);
            ProgressVM.IncrementCounter();
            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, "Metadata loaded. Downloading cover art."));

            // Step 3: Get Cover Art
            await FetchCover(newCodex);
            ProgressVM.IncrementCounter();

            //Complete import
            MainViewModel.CollectionVM.CurrentCollection.AllCodices.Add(newCodex);
            MainViewModel.CollectionVM.FilterVM.ReFilter();

            string logMsg = $"Imported {newCodex.Title}";
            Logger.Info(logMsg);
            ProgressVM.AddLogEntry(new LogEntry(LogEntry.MsgType.Info, logMsg));


            if (ShowEditWhenDone)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CodexEditWindow editWindow = new(new CodexEditViewModel(newCodex))
                    {
                        Topmost = true,
                        Owner = Application.Current.MainWindow
                    };
                    editWindow.ShowDialog();
                });
            }
        }

        public override Codex SetTags(Codex codex)
        {
            //Based on URL
            foreach (var folderTagPair in TargetCollection.Info.FolderTagPairs)
            {
                if (codex.SourceURL.Contains(folderTagPair.Folder))
                {
                    codex.Tags.AddIfMissing(folderTagPair.Tag);
                }
            }
            return codex;
        }
        #endregion

        #region Input URL Window Stuff
        protected ImportURLWindow importURLwindow;
        public abstract string ImportTitle { get; }
        public abstract string ExampleURL { get; }

        public virtual bool ShowValidateDisableCheckbox => false;
        public bool ValidateURL { get; set; } = true;
        //checkbox to disable validation so need inverted prop
        public bool DontValidateURL
        {
            get => !ValidateURL;
            set => ValidateURL = !value;
        }

        private string _inputURL = "";
        public string InputURL
        {
            get => _inputURL;
            set => SetProperty(ref _inputURL, value);
        }

        private string _importError = "";
        public string ImportError
        {
            get => _importError;
            set => SetProperty(ref _importError, value);
        }

        public bool ShowEditWhenDone { get; set; } = false;

        public virtual bool ShowScannerButton => false;

        private ActionCommand _submitUrlCommand;
        public ActionCommand SubmitURLCommand => _submitUrlCommand ??= new(SubmitURL);
        public void SubmitURL()
        {
            if (!InputURL.Contains(ExampleURL) && ValidateURL)
            {
                ImportError = $"'{InputURL}' is not a valid URL for {ImportTitle}";
                return;
            }
            if (!Utils.PingURL())
            {
                ImportError = "You need to be connected to the internet to import on online source.";
                return;
            }
            importURLwindow.Close();

            ProgressVM.ResetCounter();
            ProgressVM.Text = "Importing, Step: ";
            //3 steps: 1. connect to site, 2. get metadata, 3. get Cover
            ProgressVM.TotalAmount = 3;

            ProgressWindow progressWindow = GetProgressWindow();
            progressWindow.Show();

            Task.Run(ImportURL);
        }
        #endregion

        #region methods when scraping for metadata
        public async Task<HtmlDocument> ScrapeSite(string url)
        {
            HtmlWeb web = new();
            HtmlDocument doc;
            try
            {
                doc = await Task.Run(() => web.Load(url));
            }

            catch (Exception ex)
            {
                //fails if URL could not be loaded
                ProgressVM.AddLogEntry(new(LogEntry.MsgType.Error, ex.Message));
                Logger.Error($"Could not load {url}", ex);
                return null;
            }

            ProgressVM.IncrementCounter();

            if (doc.ParsedText is null || doc.DocumentNode is null)
            {
                LogEntry entry = new(LogEntry.MsgType.Error, $"{InputURL} could not be reached");
                ProgressVM.AddLogEntry(entry);
                Logger.Error($"{url} does not have any content", new ArgumentNullException());
                return null;
            }
            else
            {
                LogEntry entry = new(LogEntry.MsgType.Info, "File loaded");
                ProgressVM.AddLogEntry(entry);
                return doc;
            }
        }

        protected Codex SetWebScrapeHeaderMetadata(Codex codex, HtmlNode src)
        {
            codex.Title = src.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null) ?? codex.Title;
            string author = src.SelectSingleNode("//meta[@property='og:author']")?.GetAttributeValue("content", String.Empty);
            if (!String.IsNullOrEmpty(author))
            {
                codex.Authors = new() { author };
            }
            codex.Description = src.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", null) ?? codex.Description;

            return codex;
        }
        #endregion
    }
}
