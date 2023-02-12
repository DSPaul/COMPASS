using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using static COMPASS.Models.Enums;

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
                    worker = new() { WorkerReportsProgress = true };
                    worker.DoWork += ImportFiles;
                    worker.ProgressChanged += ProgressChanged;
                    worker.RunWorkerAsync();
                    worker.RunWorkerCompleted += WorkerComplete;
                    break;
                case Sources.Folder:
                    //Start new threat (so program doesn't freeze while importing)
                    worker = new() { WorkerReportsProgress = true };
                    worker.DoWork += ImportFolder;
                    worker.ProgressChanged += ProgressChanged;
                    worker.RunWorkerAsync();
                    worker.RunWorkerCompleted += WorkerComplete;
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

            WorkerComplete(null, null);
        }

        #region Properties
        private readonly CodexCollection _codexCollection;

        public Sources Source { get; init; }
        private BackgroundWorker worker;
        private ImportURLWindow importURLwindow;

        private const Sources Webscrape = Sources.Homebrewery | Sources.GmBinder | Sources.GoogleDrive;
        private const Sources APIAccess = Sources.ISBN;

        //progress window props
        private float _progressPercentage;
        public float ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        private int _importcounter;
        private string _importTextNoun = "";
        public int ImportAmount { get; private set; }
        public string ProgressText => $"Import in Progress: {_importTextNoun} {_importcounter + 1} / {ImportAmount}";

        //import url props
        public string PreviewURL { get; set; }

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

        public string ImportTitle { get; set; }

        private string _importError = "";
        public string ImportError
        {
            get => _importError;
            set => SetProperty(ref _importError, value);
        }

        private ObservableCollection<LogEntry> _log = new();
        public ObservableCollection<LogEntry> Log
        {
            get => _log;
            set => SetProperty(ref _log, value);
        }

        //folder import props

        private IEnumerable<FileTypeInfo> _toImportFiletypes;
        public IEnumerable<FileTypeInfo> ToImportFiletypes
        {
            get => _toImportFiletypes;
            set => SetProperty(ref _toImportFiletypes, value);
        }

        #endregion

        //helper class for file type selection during folder import
        public class FileTypeInfo
        {
            public FileTypeInfo(string extension, int fileCount, bool shouldImport)
            {
                FileExtension = extension;
                _fileCount = fileCount;
                ShouldImport = shouldImport;
            }

            private readonly int _fileCount;
            public string FileExtension { get; }
            public bool ShouldImport { get; set; }
            public string DisplayText => $"{FileExtension} ({_fileCount} file{(_fileCount > 1 ? @"s" : @"")})";
        }

        #region Functions and Events

        public void ImportFiles(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker importWorker = sender as BackgroundWorker;

            OpenFileDialog openFileDialog = new()
            {
                AddExtension = false,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProgressWindow progressWindow;
                _importTextNoun = "File";

                //needed to avoid error "The calling Thread must be STA" when creating progress window
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        progressWindow = new(this)
                        {
                            Owner = Application.Current.MainWindow
                        };
                        progressWindow.Show();
                    });

                //init progress tracking variables
                _importcounter = 0;
                ImportAmount = openFileDialog.FileNames.Length;
                importWorker.ReportProgress(_importcounter);

                foreach (string path in openFileDialog.FileNames)
                {
                    ImportFilePath(path);

                    //Update Progress Bar when done
                    _importcounter++;
                    importWorker.ReportProgress(_importcounter);
                }
            }
        }

        public void ImportFolder(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker importWorker = sender as BackgroundWorker;
            VistaFolderBrowserDialog openFolderDialog = new();

            if (openFolderDialog.ShowDialog() == true)
            {
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

                //find how many files of each filetype
                ImportAmount = toImport.Count;
                var toImport_grouped = toImport.GroupBy(Path.GetExtension);
                ToImportFiletypes = toImport_grouped.Select(x => new FileTypeInfo(x.Key, x.Count(), true)).ToList();

                //open window to let user choose which filetypes to import
                ImportFolderWindow importFolderWindow;
                var dialogresult = Application.Current.Dispatcher.Invoke(() =>
                {
                    importFolderWindow = new(this)
                    {
                        Owner = Application.Current.MainWindow
                    };
                    return importFolderWindow.ShowDialog();
                });

                if (dialogresult != true) return;

                //Make new toImport with only selected Filetypes
                toImport = new List<string>();
                foreach (var filetypeHelper in ToImportFiletypes)
                {
                    if (filetypeHelper.ShouldImport)
                    {
                        toImport.AddRange(toImport_grouped.First(g => g.Key == filetypeHelper.FileExtension));
                    }
                }

                //init progress tracking variables
                ProgressWindow progressWindow;
                _importcounter = 0;
                ImportAmount = toImport.Count;
                _importTextNoun = "File";
                //needed to avoid error "The calling Thread must be STA" when creating progress window
                Application.Current.Dispatcher.Invoke(() =>
                {
                    progressWindow = new(this)
                    {
                        Owner = Application.Current.MainWindow
                    };
                    progressWindow.Show();
                });

                importWorker.ReportProgress(_importcounter);

                foreach (string path in toImport)
                {
                    ImportFilePath(path);

                    //Update Progress Bar when done
                    _importcounter++;
                    importWorker.ReportProgress(_importcounter);
                }
            }
        }

        public void ImportFilePath(string path)
        {
            LogEntry logEntry = null;
            //Import File
            if (_codexCollection.AllCodices.All(p => p.Path != path))
            {
                logEntry = new(LogEntry.MsgType.Info, $"Importing {Path.GetFileName(path)}");
                worker.ReportProgress(_importcounter, logEntry);

                Codex newCodex = new(_codexCollection)
                {
                    Path = path
                };

                string FileType = Path.GetExtension(path);
                string FileName = Path.GetFileName(path);

                switch (FileType)
                {
                    case ".pdf":
                        try
                        {
                            PdfDocument pdfdoc = new(new PdfReader(path));
                            var info = pdfdoc.GetDocumentInfo();
                            newCodex.Title = !String.IsNullOrEmpty(info.GetTitle()) ? info.GetTitle() : Path.GetFileNameWithoutExtension(path);
                            newCodex.Authors = new() { info.GetAuthor() };
                            newCodex.PageCount = pdfdoc.GetNumberOfPages();
                            pdfdoc.Close();
                        }

                        catch (Exception ex)
                        {
                            Logger.log.Error(ex.InnerException);
                            //in case pdf is corrupt: PdfReader will throw error
                            //in those cases: import the pdf without opening it
                            newCodex.Title = Path.GetFileNameWithoutExtension(path);
                            logEntry = new(LogEntry.MsgType.Warning, $"Failed to read metadata from {FileName}");
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
                    logEntry = new(LogEntry.MsgType.Warning, $"Failed to generate thumbnail from {FileName}");
                    worker.ReportProgress(_importcounter, logEntry);
                }
                _codexCollection.AllCodices.Add(newCodex);
            }

            else
            {
                //if already in collection, skip
                logEntry = new(LogEntry.MsgType.Warning, $"Skipped {Path.GetFileName(path)}, already imported");
                worker.ReportProgress(_importcounter, logEntry);
            }
        }

        private void ImportManual()
        {
            CodexEditWindow editWindow = new(new CodexEditViewModel(null));
            editWindow.ShowDialog();
            editWindow.Topmost = true;
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

            importURLwindow = new(this);
            importURLwindow.Show();
        }

        private ActionCommand _submitUrlCommand;
        public ActionCommand SubmitURLCommand => _submitUrlCommand ??= new(SubmitURL);
        public void SubmitURL()
        {
            if (!InputURL.Contains(PreviewURL) && ValidateURL)
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
            worker = new() { WorkerReportsProgress = true };
            if (Webscrape.HasFlag(Source)) worker.DoWork += ImportURL;
            if (APIAccess.HasFlag(Source)) worker.DoWork += ImportFromAPI;
            worker.ProgressChanged += ProgressChanged;
            worker.RunWorkerAsync();
        }

        private ActionCommand _OpenBarcodeScannerCommand;
        public ActionCommand OpenBarcodeScannerCommand => _OpenBarcodeScannerCommand ??= new(OpenBarcodeScanner);
        public void OpenBarcodeScanner()
        {
            BarcodeScanWindow bcScanWindow = new();
            if (bcScanWindow.ShowDialog() == true)
            {
                InputURL = bcScanWindow.DecodedString;
            }
        }

        private void ImportURL(object sender, DoWorkEventArgs e)
        {
            ProgressWindow progressWindow;
            _importTextNoun = "Step";
            _importcounter = 0;
            //3 steps: 1. connect to site, 2. get metadata, 3. get Cover
            ImportAmount = 3;

            //needed to avoid error "The calling Thread must be STA" when creating progress window
            Application.Current.Dispatcher.Invoke(() =>
            {
                progressWindow = new(this);
                progressWindow.Show();
            });

            worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Info, $"Connecting to {ImportTitle}"));

            //Webscraper for metadata using HtmlAgilityPack
            HtmlWeb web = new();
            HtmlDocument doc;

            try
            {
                //Load site, set URL here
                doc = Source switch
                {
                    Sources.DnDBeyond => web.Load(string.Concat(InputURL, "/credits")),
                    _ => web.Load(InputURL),
                };
            }
            catch (Exception ex)
            {
                //fails if URL could not be loaded
                worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Error, ex.Message));
                return;
            }

            if (doc.ParsedText is null)
            {
                worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Error, $"{ImportTitle} could not be reached"));
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
                    newFile.Authors = new(metadata.SelectToken("brew.authors")?.Values<string>());
                    newFile.Version = (string)metadata.SelectToken("brew.version");
                    newFile.PageCount = (int)metadata.SelectToken("brew.pageCount");
                    newFile.Description = (string)metadata.SelectToken("brew.description");
                    newFile.ReleaseDate = DateTime.Parse(((string)metadata.SelectToken("brew.createdAt"))?.Split('T')[0], CultureInfo.InvariantCulture);
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
            worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Info, "Metadata loaded. Downloading cover art."));

            //Get Cover Art
            CoverFetcher.GetCoverFromURL(newFile, Source);

            //add file to co
            _codexCollection.AllCodices.Add(newFile);
            RaisePropertyChanged("FilteredCodices");

            //import done
            _importcounter++;
            worker.ReportProgress(_importcounter);

            Application.Current.Dispatcher.Invoke(MVM.Refresh);
        }

        private void ImportFromAPI(object sender, DoWorkEventArgs e)
        {
            ProgressWindow progressWindow;
            _importTextNoun = "Step";
            _importcounter = 0;
            //3 steps: 1. connect to api, 2. get metadata, 3. get Cover
            ImportAmount = 3;

            //needed to avoid error "The calling Thread must be STA" when creating progress window
            Application.Current.Dispatcher.Invoke(() =>
            {
                progressWindow = new(this);
                progressWindow.Show();
            });

            worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Info, "Fetching Data"));
            string uri = Source switch
            {
                Sources.ISBN => $"http://openlibrary.org/api/books?bibkeys=ISBN:{InputURL.Trim('-', ' ')}&format=json&jscmd=details",
                _ => null
            };

            JObject metadata = Task.Run(async () => await Utils.GetJsonAsync(uri)).Result;

            if (!metadata.HasValues)
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
                        newFile.Authors = new(details.SelectToken("authors").Select(item => item.SelectToken("name").ToString()));
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
            RaisePropertyChanged("FilteredCodices");

            //import done
            _importcounter++;
            worker.ReportProgress(_importcounter);

            Application.Current.Dispatcher.Invoke(MVM.Refresh);
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //calculate current percentage for progressbar
            ProgressPercentage = (int)((float)_importcounter / ImportAmount * 100);
            //update text
            RaisePropertyChanged(nameof(ProgressText));
            //write log entry if any
            if (e.UserState is LogEntry logEntry) Log.Add(logEntry);
        }

        private void WorkerComplete(object sender, EventArgs e)
        {
            _codexCollection.PopulateMetaDataCollections();
            MVM.Refresh();
        }
        #endregion

    }
}
