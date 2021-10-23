using COMPASS.Models;
using COMPASS.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static COMPASS.Tools.Enums;
using iText;
using iText.Kernel.Pdf;
using HtmlAgilityPack;
using System.Globalization;
using System.Collections.ObjectModel;
using COMPASS.ViewModels.Commands;
using OpenQA.Selenium.Chrome;

namespace COMPASS.ViewModels
{
    public class ImportViewModel : ObservableObject
    {
        public ImportViewModel(MainViewModel vm, ImportMode importmode)
        {
            //set codexCollection so we know where to import to
            _codexCollection = vm.CurrentCollection;

            mode = importmode;
            SubmitURLCommand = new BasicCommand(SubmitURL);

            //Only needed for Reset method
            MVM = vm;

            //Call Relevant function
            switch (mode)
            {
                case ImportMode.Pdf:
                    //Start new threat (so program doesn't freeze while importing)
                    worker = new BackgroundWorker { WorkerReportsProgress = true };
                    worker.DoWork += ImportFromPdf;
                    worker.ProgressChanged += ProgressChanged;
                    worker.RunWorkerAsync();
                    break;
                case ImportMode.Manual:
                    ImportManual();
                    break;
                case ImportMode.GmBinder:
                    OpenImportURLDialog();
                    break;
                case ImportMode.Homebrewery:
                    OpenImportURLDialog();
                    break;
                case ImportMode.DnDBeyond:
                    OpenImportURLDialog();
                    break;
            }  
        }

        #region Properties
        readonly MainViewModel MVM;

        private CodexCollection _codexCollection;

        private ImportMode mode;
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
                        CoverArtGenerator.ConvertPDF(pdf, _codexCollection.Folder);
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
                MVM.Reset();
            });
            if(SelectWhenDone!=null) MVM.CurrentFileViewModel.SelectedFile = SelectWhenDone;
        }

        private void ImportManual()
        {
            MVM.CurrentEditViewModel = new FileEditViewModel(MVM,null);
            FilePropWindow fpw = new FilePropWindow((FileEditViewModel)MVM.CurrentEditViewModel);
            fpw.ShowDialog();
            fpw.Topmost = true;
        }

        public void OpenImportURLDialog()
        {
            switch (mode)
            {
                case ImportMode.GmBinder:
                    ImportTitle = "GM Binder:";
                    PreviewURL = "https://www.gmbinder.com/share/";
                    break;
                case ImportMode.Homebrewery:
                    ImportTitle = "Homebrewery";
                    PreviewURL = "https://homebrewery.naturalcrit.com/share/";
                    break;
                case ImportMode.DnDBeyond:
                    ImportTitle = "D&D Beyond";
                    PreviewURL = "https://www.dndbeyond.com/sources/";
                    break;
            }

            iURLw = new ImportURLWindow(this);
            iURLw.Show();
        }

        public BasicCommand SubmitURLCommand { get; private set; }
        public void SubmitURL()
        {
            if (!InputURL.Contains(PreviewURL))
            {
                ImportError = String.Format("{0} is not a valid URL for {1}", InputURL, ImportTitle);
                return;
            }
            if (!MVM.pingURL())
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
            switch (mode)
            {
                case ImportMode.DnDBeyond:
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
            switch (mode)
            {
                case ImportMode.GmBinder:
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

                case ImportMode.Homebrewery:
                    //Set known metadata
                    newFile.Publisher = "Homebrewery";

                    //Scrape metadata
                    //Select script tag with all metadata
                    HtmlNode script = src.SelectSingleNode("/html/body/script[2]");
                    List<string> BrewInfo = script.InnerText.Split(',').Skip(2).ToList();

                    //Cut of end starting from text(bin)
                    int lastIndex = BrewInfo.FindIndex(text => text.Contains("\"text"));
                    BrewInfo = BrewInfo.GetRange(0, lastIndex);
                    //cut of "brew":{ from start
                    BrewInfo[0] = BrewInfo[0].Substring(8);

                    //make temp dictionary out of list, chars like " [ ]  still need to be removed
                    Dictionary<string, string> tempBrewInfoDict = BrewInfo.Select(item => item.Split(':')).ToDictionary(s => s[0], s => s[1]);
                    //make clean dict to use
                    Dictionary<string, string> BrewInfoDict = new Dictionary<string, string>();
                    foreach(KeyValuePair<string,string> info in tempBrewInfoDict)
                    {
                        var newkey = info.Key.Trim('"');
                        var newval = info.Value.Trim(new Char[] { '"', '[', ']' });
                        BrewInfoDict.Add(newkey, newval);
                    }
                    if (BrewInfoDict.ContainsKey("title"))      newFile.Title = BrewInfoDict["title"];
                    if (BrewInfoDict.ContainsKey("authors"))    newFile.Author = BrewInfoDict["authors"];
                    if (BrewInfoDict.ContainsKey("description")) newFile.Description = BrewInfoDict["description"];
                    if (BrewInfoDict.ContainsKey("version"))    newFile.Version = BrewInfoDict["version"];

                    if (BrewInfoDict.ContainsKey("createdAt"))  newFile.ReleaseDate = DateTime.Parse(BrewInfoDict["createdAt"].Split('T')[0], CultureInfo.InvariantCulture);                            

                    //get pagecount
                    HtmlNode pageInfo = src.SelectSingleNode("//div[@class='pageInfo']");
                    newFile.PageCount = int.Parse(pageInfo.InnerText.Split('/')[1]);
                    break;

                case ImportMode.DnDBeyond:
                    //Set known metadata
                    newFile.Publisher = "D&D Beyond";
                    newFile.Author = "Wizards of the Coast";

                    //Scrape metadata by going to storepage, get to storepage by using that /credits redirects there
                    //Doesn't work because DnD Beyond detects bots/scrapers
                    newFile.Title = src.SelectSingleNode("//meta[@property='og:title']").GetAttributeValue("content", String.Empty);
                    newFile.Description = src.SelectSingleNode("//meta[@property='og:description']").GetAttributeValue("content", String.Empty);
                    break;
            }

            //Scraping complete
            _importcounter++;
            worker.ReportProgress(_importcounter, new LogEntry(LogEntry.MsgType.Info, "Metadata loaded. Generating cover art."));

            //Get Cover Art
            CoverArtGenerator.GetCoverFromURL(InputURL, newFile, mode);

            //add file to cc
            _codexCollection.AllFiles.Add(newFile);
            RaisePropertyChanged("_codexCollection.ActiveFiles");

            //import done
            _importcounter++;
            worker.ReportProgress(_importcounter);
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
