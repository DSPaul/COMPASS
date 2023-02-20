using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using HtmlAgilityPack;
using System;
using System.ComponentModel;

namespace COMPASS.ViewModels.Sources
{
    public abstract class OnlineSourceViewModel : SourceViewModel
    {
        #region Import logic
        public override void Import()
        {
            importURLwindow = new(this);
            importURLwindow.Show();
        }

        public void ImportURL(object sender, DoWorkEventArgs e)
        {
            Codex newCodex = new(MainViewModel.CollectionVM.CurrentCollection)
            {
                SourceURL = InputURL
            };

            // Steps 1 & 2: Load Source and Scrape metadata
            newCodex = SetMetaData(newCodex);
            ProgressCounter++;
            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, "Metadata loaded. Downloading cover art."));

            // Step 3: Get Cover Art
            FetchCover(newCodex);
            ProgressCounter++;

            //Complete import
            MainViewModel.CollectionVM.CurrentCollection.AllCodices.Add(newCodex);
            worker.ReportProgress(ProgressCounter);
        }
        #endregion

        #region Input URL Window Stuff
        protected ImportURLWindow importURLwindow;
        public abstract string ImportTitle { get; }
        public abstract string ExampleURL { get; }

        public override string ProgressText => $"Import in Progress: Step {ProgressCounter + 1} / {ImportAmount}";

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

            ProgressCounter = 0;
            //3 steps: 1. connect to site, 2. get metadata, 3. get Cover
            ImportAmount = 3;

            ProgressWindow progressWindow = GetProgressWindow();
            progressWindow.Show();

            InitWorker(ImportURL);
            worker.RunWorkerAsync();
        }


        #endregion

        #region methods when scraping for metadata
        public HtmlDocument ScrapeSite(string url)
        {
            HtmlWeb web = new();
            HtmlDocument doc;
            try
            {
                doc = web.Load(url);
            }

            catch (Exception ex)
            {
                //fails if URL could not be loaded
                worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Error, ex.Message));
                return null;
            }

            ProgressCounter++;

            if (doc.ParsedText is null || doc.DocumentNode is null)
            {
                LogEntry entry = new(LogEntry.MsgType.Error, $"{InputURL} could not be reached");
                worker.ReportProgress(ProgressCounter, entry);
                return null;
            }
            else
            {
                LogEntry entry = new(LogEntry.MsgType.Info, "File loaded");
                worker.ReportProgress(ProgressCounter, entry);
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
