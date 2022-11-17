using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using COMPASS.Windows;
using HtmlAgilityPack;
using iText.Kernel.Pdf;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class ImportViewModel : ViewModelBase
    {
        public ImportViewModel(Sources source, CodexCollection collection)
        {
            //set codexCollection so we know where to import to
            _codexCollection = collection;

            Source = source;

            //Call Relevant function
            switch (source)
            {
                case Sources.File:
                    //Start new threat (so program doesn't freeze while importing)
                    worker = new BackgroundWorker { WorkerReportsProgress = true };
                    worker.DoWork += ImportFiles;
                    worker.ProgressChanged += ProgressChanged;
                    worker.RunWorkerAsync();
                    break;
                case Sources.Folder:
                    //Start new threat (so program doesn't freeze while importing)
                    worker = new BackgroundWorker { WorkerReportsProgress = true };
                    worker.DoWork += ImportFolder;
                    worker.ProgressChanged += ProgressChanged;
                    worker.RunWorkerAsync();
                    break;
                case Sources.Manual:
                    ImportManual();
                    break;
                case Sources.ISBN:
                    OpenImportURLDialog();
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

        public Sources Source { get; init; }
        private BackgroundWorker worker;
        private ImportURLWindow iURLw;

        private const Sources Webscrape = Sources.Homebrewery | Sources.GmBinder | Sources.GoogleDrive;
        private const Sources APIAccess = Sources.ISBN;

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

        private ObservableCollection<LogEntry> _log = new();
        public ObservableCollection<LogEntry> Log
        {
            get { return _log; }
            set { SetProperty(ref _log, value); }
        }

        #endregion

        #region Functions and Events

        public void ImportFiles(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            OpenFileDialog openFileDialog = new()
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
                        pgw = new ProgressWindow(this)
                        {
                            Owner = Application.Current.MainWindow
                        };
                        pgw.Show();
                    });
                
                //init progress tracking variables
                _importcounter = 0;
                _importamount = openFileDialog.FileNames.Length;
                worker.ReportProgress(_importcounter);

                foreach (string path in openFileDialog.FileNames)
                {
                    ImportFilePath(path);

                    //Update Progress Bar when done
                    _importcounter++;
                    worker.ReportProgress(_importcounter);
                }
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MVM.Refresh();
            });
            if(SelectWhenDone!=null) MVM.CurrentLayout.SelectedFile = SelectWhenDone;
        }

        public void ImportFolder(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            VistaFolderBrowserDialog openFolderDialog = new();

            if (openFolderDialog.ShowDialog() == true)
            {
                ProgressWindow pgw;
                _importtype = "File";

                //needed to avoid error "The calling Thread must be STA" when creating progress window
                Application.Current.Dispatcher.Invoke(() =>
                {
                    pgw = new ProgressWindow(this)
                    {
                        Owner = Application.Current.MainWindow
                    };
                    pgw.Show();
                });

                //find files in folder, including subfolder
                List<string> toSearch = new(openFolderDialog.SelectedPaths); //list with folders to search
                List<string> toImport = new(); //list with files to import

                while (toSearch.Count > 0)
                {
                    string currentFolder = toSearch[0];

                    //find subfolder to include in search
                    toSearch.AddRange(Directory.GetDirectories(currentFolder));
                    //add files in folder to import list
                    toImport.AddRange(Directory.GetFiles(currentFolder));
                    //done with folder, remove it from list
                    toSearch.Remove(currentFolder);
                }

                //init progress tracking variables
                _importcounter = 0;
                _importamount = toImport.Count;
                worker.ReportProgress(_importcounter);

                foreach (string path in toImport)
                {
                    ImportFilePath(path);

                    //Update Progress Bar when done
                    _importcounter++;
                    worker.ReportProgress(_importcounter);
                }
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MVM.Refresh();
            });
        }

        public void ImportFilePath(string path)
        {
            LogEntry logEntry = null;
            //Import File
            if (_codexCollection.AllCodices.All(p => p.Path != path))
            {
                logEntry = new LogEntry(LogEntry.MsgType.Info, string.Format("Importing {0}", Path.GetFileName(path)));
                worker.ReportProgress(_importcounter, logEntry);

                Codex newCodex = new(_codexCollection)
                {
                    Path = path
                };

                string FileType = Path.GetExtension(path);

                switch (FileType)
                {
                    case ".pdf":
                        try
                        {
                            PdfDocument pdfdoc = new(new PdfReader(path));
                            var info = pdfdoc.GetDocumentInfo();
                            newCodex.Title = info.GetTitle() ?? Path.GetFileNameWithoutExtension(path);
                            newCodex.Authors = new() { info.GetAuthor() };
                            newCodex.PageCount = pdfdoc.GetNumberOfPages();
                            pdfdoc.Close();
                        }

                        catch(Exception ex)
                        {
                            Logger.log.Error(ex.InnerException);
                            //in case pdf is corrupt: PdfReader will throw error
                            //in those cases: import the pdf without opening it
                            newCodex.Title = Path.GetFileNameWithoutExtension(path);
                            logEntry = new LogEntry(LogEntry.MsgType.Warning, string.Format("Failed to read metadata from {0}", Path.GetFileName(path)));
                            worker.ReportProgress(_importcounter, logEntry);
                        }
                        
                        break;

                    default:
                        newCodex.Title = Path.GetFileNameWithoutExtension(path);
                        break;
                }

                bool succes = CoverFetcher.GetCoverFromFile(newCodex);
                if (!succes)
                {
                    logEntry = new LogEntry(LogEntry.MsgType.Warning, string.Format("Failed to generate thumbnail from {0}", Path.GetFileName(path)));
                    worker.ReportProgress(_importcounter, logEntry);
                }
                _codexCollection.AllCodices.Add(newCodex);
                //SelectWhenDone = newCodex;

            }

            else
            {
                //if already in collection, skip
                logEntry = new LogEntry(LogEntry.MsgType.Warning, string.Format("Skipped {0}, already imported", Path.GetFileName(path)));
                worker.ReportProgress(_importcounter, logEntry);
            }
        }

        private void ImportManual()
        {
            FilePropWindow fpw = new( new CodexEditViewModel(null));
            fpw.ShowDialog();
            fpw.Topmost = true;
        }

        public void OpenImportURLDialog()
        {
            switch (Source)
            {
                case Sources.ISBN:
                    ImportTitle = "ISBN:";
                    PreviewURL = "";
                    break;
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

        private ActionCommand _submitUrlCommand;
        public ActionCommand SubmitURLCommand => _submitUrlCommand ??= new(SubmitURL);
        public void SubmitURL()
        {
            if (!InputURL.Contains(PreviewURL))
            {
                ImportError = String.Format("{0} is not a valid URL for {1}", InputURL, ImportTitle);
                return;
            }
            if (!Utils.PingURL())
            {
                ImportError = String.Format("You need to be connected to the internet to import on online source.");
                return;
            }
            iURLw.Close();
            worker = new BackgroundWorker { WorkerReportsProgress = true };
            if (Webscrape.HasFlag(Source)) worker.DoWork += ImportURL;
            if (APIAccess.HasFlag(Source)) worker.DoWork += ImportFromAPI;
            worker.ProgressChanged += ProgressChanged;
            worker.RunWorkerAsync();
        }

        private ActionCommand _OpenBarcodeScannerCommand;
        public ActionCommand OpenBarcodeScannerCommand => _OpenBarcodeScannerCommand ??= new(OpenBarcodeScanner);
        public void OpenBarcodeScanner()
        {
            var bcScanWindow = new BarcodeScanWindow();
            if( bcScanWindow.ShowDialog() == true)
            {
                InputURL = bcScanWindow.DecodedString;
            };
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
            HtmlWeb web = new();

            //Load site, set URL here
            HtmlDocument doc = Source switch
            {
                Sources.DnDBeyond => web.Load(string.Concat(InputURL, "/credits")),
                _ => web.Load(InputURL),
            };

            if (doc.ParsedText == null)
            {
                worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Error, String.Format("{0} could not be reached", ImportTitle)));
                return;
            }

            HtmlNode src = doc.DocumentNode;


            Codex newFile = new(_codexCollection)
            {
                SourceURL = InputURL
            };

            //loading complete
            _importcounter++;
            worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Info, "File loaded, scraping metadata"));

            //Start scraping metadata, website specific
            switch (Source)
            {
                case Sources.ISBN:
                    newFile.Publisher = "";
                    break;

                case Sources.GmBinder:
                    //Set known metadata
                    newFile.Publisher = "GM Binder";

                    //Scrape metadata
                    newFile.Title = src.SelectSingleNode("//html/head/title").InnerText.Split('|')[0];
                    newFile.Authors = new() { src.SelectSingleNode("//meta[@property='og:author']").GetAttributeValue("content", String.Empty) };

                    //get pagecount
                    HtmlNode previewDiv = doc.GetElementbyId("preview");
                    IEnumerable<HtmlNode> pages = previewDiv.ChildNodes.Where(node => node.Id.Contains('p'));
                    newFile.PageCount = pages.Count();
                    break;

                case Sources.Homebrewery:
                    //Set known metadata
                    newFile.Publisher = "Homebrewery";

                    //Scrape metadata
                    //Select script tag with all metadata in JSON format
                    string script = src.SelectSingleNode("/html/body/script[2]").InnerText;
                    //json is encapsulated by "start_app() function, so cut that out
                    string rawData = script[10..^1];
                    JObject metadata = JObject.Parse(rawData);

                    newFile.Title = (string)metadata.SelectToken("brew.title");
                    newFile.Authors = new(metadata.SelectToken("brew.authors").Values<string>());
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
                    newFile.Authors = new() { "Wizards of the Coast" };

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
            _codexCollection.AllCodices.Add(newFile);
            RaisePropertyChanged("ActiveFiles");

            //import done
            _importcounter++;
            worker.ReportProgress(_importcounter);

            Application.Current.Dispatcher.Invoke(() =>
            {
                MVM.Refresh();
            });
        }
        
        private void ImportFromAPI(object sender, DoWorkEventArgs e)
        {
            ProgressWindow pgw;
            _importtype = "Step";
            _importcounter = 0;
            //3 steps: 1. connect to api, 2. get metadata, 3. get Cover
            _importamount = 3;

            //needed to avoid error "The calling Thread must be STA" when creating progress window
            Application.Current.Dispatcher.Invoke(() =>
            {
                pgw = new ProgressWindow(this);
                pgw.Show();
            });

            worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Info, String.Format("Fetching Data")));
            string uri = Source switch
            {
                Sources.ISBN => $"http://openlibrary.org/api/books?bibkeys=ISBN:{InputURL.Trim('-',' ')}&format=json&jscmd=details",
            _ => null
            };

            JObject metadata = Task.Run(async () => await Utils.GetJsonAsync(uri)).Result;

            if (metadata.HasValues == false)
            {
                string message = $"ISBN {InputURL} was not found on openlibrary.org \n" +
                    $"You can contribute by submitting this book at \n" +
                    $"https://openlibrary.org/books/add";
                worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Error, message));
                return;
            }

            Codex newFile = new(_codexCollection);

            //loading complete
            _importcounter++;
            worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Info, "File loaded, parsing metadata"));

            //Start parsing json
            switch (Source)
            {
                case Sources.ISBN:
                    var details = metadata.First.First.SelectToken("details");
                    newFile.Title = (string)details.SelectToken("title");
                    if (details.SelectToken("authors") != null)
                        newFile.Authors = new ObservableCollection<string>( details.SelectToken("authors").Select(item => item.SelectToken("name").ToString()));
                    if (details.SelectToken("pagination") != null)
                    {
                        newFile.PageCount = Int32.Parse(Regex.Match(details.SelectToken("pagination").ToString(), @"\d+").Value);
                    }
                    newFile.PageCount = (int?)details.SelectToken("number_of_pages") ?? newFile.PageCount;
                    newFile.Publisher = (string)details.SelectToken("publishers[0]") ?? newFile.Publisher;
                    newFile.Description = (string)details.SelectToken("description.value") ?? newFile.Description;
                    DateTime tempDate;
                    if (DateTime.TryParse((string)details.SelectToken("publish_date"), out tempDate))
                        newFile.ReleaseDate = tempDate;
                    newFile.ISBN = InputURL;
                    newFile.Physically_Owned = true;
                    break;
            }

            //Scraping complete
            _importcounter++;
            worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Info, "Metadata loaded. Fetching cover art."));

            //Get Cover Art
            CoverFetcher.GetCoverFromISBN(newFile);

            //add file to cc
            _codexCollection.AllCodices.Add(newFile);
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
            RaisePropertyChanged(nameof(ProgressText));
            //write log entry if any
            if (e.UserState is LogEntry logEntry) Log.Add(logEntry);
        }
        #endregion
    
    }
}
