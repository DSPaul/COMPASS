using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using COMPASS.Windows;
using HtmlAgilityPack;
using iText.Kernel.Pdf;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class ImportViewModel : BaseViewModel
    {
        public ImportViewModel(Sources source)
        {
            //set codexCollection so we know where to import to
            _codexCollection = MVM.CurrentCollection;

            Source = source;
            SubmitURLCommand = new ActionCommand(SubmitURL);

            //Call Relevant function
            switch (source)
            {
                case Sources.Pdf:
                    //Start new threat (so program doesn't freeze while importing)
                    worker = new BackgroundWorker { WorkerReportsProgress = true };
                    worker.DoWork += ImportFromPdf;
                    worker.ProgressChanged += ProgressChanged;
                    worker.RunWorkerAsync();
                    break;
                case Sources.Manual:
                    ImportManual();
                    break;
                case Sources.GmBinder:
                    OpenImportURLDialog();
                    break;
                case Sources.Homebrewery:
                    OpenImportURLDialog();
                    break;
                case Sources.DnDBeyond:
                    OpenImportURLDialog();
                    break;
                case Sources.GoogleDrive:
                    OpenImportURLDialog();
                    break;
            }  
        }

        #region Properties
        private CodexCollection _codexCollection;

        private Sources Source;
        private BackgroundWorker worker;
        private ImportURLWindow iURLw;

        private float _progressPercentage;
        public float ProgressPercentage
        {
            get { return _progressPercentage; }
            set { SetProperty(ref _progressPercentage, value); }
        }

        private readonly string _importText = "Import in Progress: {0} {1} / {2}";
        private int _importcounter;
        private int _importamount;
        private string _importtype = "";

        public string ProgressText
        {
            get { return string.Format(_importText, _importtype,_importcounter + 1, _importamount); }
        }

        private string _previewURL;
        public string PreviewURL
        {
            get { return _previewURL; }
            set { SetProperty(ref _previewURL, value); }
        }

        private string _inputURL = "";
        public string InputURL
        {
            get { return _inputURL; }
            set { SetProperty(ref _inputURL, value); }
        }

        private string _importTitle;
        public string ImportTitle
        {
            get { return _importTitle; }
            set { SetProperty(ref _importTitle, value); }
        }

        private string _importError = "";
        public string ImportError
        {
            get { return _importError; }
            set { SetProperty(ref _importError, value); }
        }

        private ObservableCollection<LogEntry> _log = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> Log
        {
            get { return _log; }
            set { SetProperty(ref _log, value); }
        }

        #endregion

        #region Functions and Events

        public void ImportFromPdf(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AddExtension = false,
                Multiselect = true
            };

            Codex SelectWhenDone = null;

            if (openFileDialog.ShowDialog() == true)
            {
                ProgressWindow pgw;
                _importtype = "File";

                //needed to avoid error "The calling Thread must be STA" when creating progress window
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        pgw = new ProgressWindow(this);
                        pgw.Owner = Application.Current.MainWindow;
                        pgw.Show();
                    });
                
                //init progress tracking variables
                _importcounter = 0;
                _importamount = openFileDialog.FileNames.Length;
                LogEntry logEntry = null;
                worker.ReportProgress(_importcounter);

                foreach (string path in openFileDialog.FileNames)
                { 
                    //Import File
                    if (_codexCollection.AllFiles.All(p => p.Path != path))
                    {
                        logEntry = new LogEntry(LogEntry.MsgType.Info, string.Format("Importing {0}", System.IO.Path.GetFileName(path)));
                        worker.ReportProgress(_importcounter, logEntry);

                        PdfDocument pdfdoc = new PdfDocument(new PdfReader(path));
                        var info = pdfdoc.GetDocumentInfo();
                        Codex pdf = new Codex(_codexCollection)
                        {
                            Path = path,
                            Title = info.GetTitle() ?? System.IO.Path.GetFileNameWithoutExtension(path),
                            Author = info.GetAuthor(),
                            PageCount = pdfdoc.GetNumberOfPages()
                        };
                        pdfdoc.Close();
                        _codexCollection.AllFiles.Add(pdf);
                        CoverFetcher.GetCoverFromPDF(pdf);
                        SelectWhenDone = pdf;
                    }

                    else
                    {
                        //if already in collection, skip
                        logEntry = new LogEntry(LogEntry.MsgType.Info, string.Format("Skipped {0}, already imported", System.IO.Path.GetFileName(path)));
                        worker.ReportProgress(_importcounter,logEntry);
                    }

                    //Update Progress Bar when done
                    _importcounter++;
                    worker.ReportProgress(_importcounter);
                }
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MVM.Refresh();
            });
            if(SelectWhenDone!=null) MVM.CurrentFileViewModel.SelectedFile = SelectWhenDone;
        }

        private void ImportManual()
        {
            MVM.CurrentEditViewModel = new FileEditViewModel(null);
            FilePropWindow fpw = new FilePropWindow((FileEditViewModel)MVM.CurrentEditViewModel);
            fpw.ShowDialog();
            fpw.Topmost = true;
        }

        public void OpenImportURLDialog()
        {
            switch (Source)
            {
                case Sources.GmBinder:
                    ImportTitle = "GM Binder:";
                    PreviewURL = "https://www.gmbinder.com/share/";
                    break;
                case Sources.Homebrewery:
                    ImportTitle = "Homebrewery";
                    PreviewURL = "https://homebrewery.naturalcrit.com/share/";
                    break;
                case Sources.DnDBeyond:
                    ImportTitle = "D&D Beyond";
                    PreviewURL = "https://www.dndbeyond.com/sources/";
                    break;
                case Sources.GoogleDrive:
                    ImportTitle = "Google Drive";
                    PreviewURL = "https://drive.google.com/file/";
                    break;
            }

            iURLw = new ImportURLWindow(this);
            iURLw.Show();
        }

        public ActionCommand SubmitURLCommand { get; private set; }
        public void SubmitURL()
        {
            if (!InputURL.Contains(PreviewURL))
            {
                ImportError = String.Format("{0} is not a valid URL for {1}", InputURL, ImportTitle);
                return;
            }
            if (!Utils.pingURL())
            {
                ImportError = String.Format("You need to be connected to the internet to import on online source.");
                return;
            }
            iURLw.Close();
            worker = new BackgroundWorker { WorkerReportsProgress = true };
            worker.DoWork += ImportURL;
            worker.ProgressChanged += ProgressChanged;
            worker.RunWorkerAsync();
        }

        private void ImportURL(object sender, DoWorkEventArgs e)
        {
            ProgressWindow pgw;
            _importtype = "Step";
            _importcounter = 0;
            //3 steps: 1. connect to site, 2. get metadata, 3. get Cover
            _importamount = 3; 

            //needed to avoid error "The calling Thread must be STA" when creating progress window
            Application.Current.Dispatcher.Invoke(() =>
            {
                pgw = new ProgressWindow(this);
                pgw.Show();
            });

            worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Info,String.Format("Connecting to {0}", ImportTitle)));

            //Webscraper for metadata using HtmlAgilityPack
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc;
            switch (Source)
            {
                case Sources.DnDBeyond:
                    doc = web.Load(string.Concat(InputURL, "/credits"));
                    break;
                default:
                    doc = web.Load(InputURL);
                    break;
            }

            if (doc.ParsedText == null)
            {
                worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Error, String.Format("{0} could not be reached", ImportTitle)));
                return;
            }
            HtmlNode src = doc.DocumentNode;

            Codex newFile = new Codex(_codexCollection)
            {
                SourceURL = InputURL
            };

            //loading complete
            _importcounter++;
            worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Info, "File loaded, scraping metadata"));

            //Start scraping metadata, website specific
            switch (Source)
            {
                case Sources.GmBinder:
                    //Set known metadata
                    newFile.Publisher = "GM Binder";

                    //Scrape metadata
                    newFile.Title = src.SelectSingleNode("//html/head/title").InnerText.Split('|')[0];
                    newFile.Author = src.SelectSingleNode("//meta[@property='og:author']").GetAttributeValue("content", String.Empty);

                    //get pagecount
                    HtmlNode previewDiv = doc.GetElementbyId("preview");
                    IEnumerable<HtmlNode> pages = previewDiv.ChildNodes.Where(node => node.Id.Contains("p"));
                    newFile.PageCount = pages.Count();
                    break;

                case Sources.Homebrewery:
                    //Set known metadata
                    newFile.Publisher = "Homebrewery";

                    //Scrape metadata
                    //Select script tag with all metadata in JSON format
                    string script = src.SelectSingleNode("/html/body/script[2]").InnerText;
                    //json is encapsulated by "start_app() function, so cut that out
                    string rawData = script.Substring(10,script.Length-11);
                    JObject metadata = JObject.Parse(rawData);

                    newFile.Title = (string)metadata.SelectToken("brew.title");
                    newFile.Author = (string)metadata.SelectToken("brew.authors[0]");
                    newFile.Version = (string)metadata.SelectToken("brew.version");
                    newFile.PageCount = (int)metadata.SelectToken("brew.pageCount");
                    newFile.Description = (string)metadata.SelectToken("brew.description");
                    newFile.ReleaseDate = DateTime.Parse(((string)metadata.SelectToken("brew.createdAt")).Split('T')[0], CultureInfo.InvariantCulture);
                    break;

                case Sources.GoogleDrive:
                    //Set known metadata
                    newFile.Publisher = "Google Drive";

                    //Scrape metadata
                    newFile.Title = src.SelectSingleNode("//meta[@property='og:title']").GetAttributeValue("content", string.Empty);

                    break;

                case Sources.DnDBeyond:
                    //Set known metadata
                    newFile.Publisher = "D&D Beyond";
                    newFile.Author = "Wizards of the Coast";

                    //Scrape metadata by going to storepage, get to storepage by using that /credits redirects there
                    //Doesn't work because DnD Beyond detects bots/scrapers
                    newFile.Title = src.SelectSingleNode("//meta[@property='og:title']").GetAttributeValue("content", string.Empty);
                    newFile.Description = src.SelectSingleNode("//meta[@property='og:description']").GetAttributeValue("content", string.Empty);
                    break;
            }

            //Scraping complete
            _importcounter++;
            worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Info, "Metadata loaded. Fetching cover art."));

            //Get Cover Art
            CoverFetcher.GetCoverFromURL(newFile, Source);

            //add file to cc
            _codexCollection.AllFiles.Add(newFile);
            RaisePropertyChanged("ActiveFiles");

            //import done
            _importcounter++;
            worker.ReportProgress(_importcounter);

            Application.Current.Dispatcher.Invoke(() =>
            {
                MVM.Refresh();
            });
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //calculate current percentage for progressbar
            ProgressPercentage = (int)((float)_importcounter /_importamount * 100);
            //update text
            RaisePropertyChanged("ProgressText");
            //write log entry if any
            LogEntry logEntry = e.UserState as LogEntry;
            if (logEntry != null) Log.Add(logEntry);
        }
        #endregion
    }
}
