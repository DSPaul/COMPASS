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

namespace COMPASS.ViewModels
{
    public class ImportViewModel : ObservableObject
    {
        public ImportViewModel(MainViewModel vm, ImportMode mode)
        {
            //set data so we know where to import to
            _data = vm.CurrentData;

            //Only needed for Reset method
            MVM = vm;

            //Start new threat (so program doesn't freeze while importing)
            BackgroundWorker worker;

            //Call Relevant function
            switch (mode)
            {
                case ImportMode.Pdf:
                    worker= new BackgroundWorker { WorkerReportsProgress = true };
                    worker.DoWork += ImportFromPdf;
                    worker.ProgressChanged += ProgressChanged;
                    worker.RunWorkerAsync();
                    break;
                case ImportMode.Manual:
                    ImportManual();
                    break;
                case ImportMode.GmBinder:
                    worker = new BackgroundWorker { WorkerReportsProgress = true };
                    worker.DoWork += ImportURL;
                    worker.ProgressChanged += ProgressChanged;
                    worker.RunWorkerAsync(mode);
                    break;
                case ImportMode.Homebrewery:
                    worker = new BackgroundWorker { WorkerReportsProgress = true };
                    worker.DoWork += ImportURL;
                    worker.ProgressChanged += ProgressChanged;
                    worker.RunWorkerAsync(mode);
                    break;
            }  
        }

        #region Properties
        readonly MainViewModel MVM;

        private Data _data;

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
            get { return string.Format(_importText, _importtype,_importcounter, _importamount); }
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

        private ObservableCollection<LogEntry> _log = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> Log
        {
            get { return _log; }
            set { SetProperty(ref _log, value); }
        }

        #endregion

        #region Functions and Events

        private void ImportFromPdf(object sender, DoWorkEventArgs e)
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
                    if (_data.AllFiles.All(p => p.Path != path))
                    {
                        logEntry = new LogEntry(LogEntry.MsgType.Info, string.Format("Importing {0}", System.IO.Path.GetFileName(path)));
                        worker.ReportProgress(_importcounter, logEntry);

                        PdfDocument pdfdoc = new PdfDocument(new PdfReader(path));
                        var info = pdfdoc.GetDocumentInfo();
                        Codex pdf = new Codex(_data)
                        {
                            Path = path,
                            Title = info.GetTitle() ?? System.IO.Path.GetFileNameWithoutExtension(path),
                            Author = info.GetAuthor(),
                            PageCount = pdfdoc.GetNumberOfPages()
                        };
                        pdfdoc.Close();
                        _data.AllFiles.Add(pdf);
                        CoverArtGenerator.ConvertPDF(pdf, _data.Folder);
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

        private void ImportURL(object sender, DoWorkEventArgs e)
        {
            ImportMode mode = (ImportMode)e.Argument;
            switch (mode)
            {
                case ImportMode.GmBinder:
                    ImportTitle = "GM Binder URL:";
                    PreviewURL = "https://www.gmbinder.com/share/";
                    break;
                case ImportMode.Homebrewery:
                    ImportTitle = "Homebrewery URL";
                    PreviewURL = "https://homebrewery.naturalcrit.com/share/";
                    break;
            }
            ImportURLWindow popup = new ImportURLWindow(this);

            if(popup.ShowDialog() == true)
            {
                if (!InputURL.Contains(PreviewURL))
                {
                    //TODO add error: "is not a valid *source* URL
                    return;
                }
                if (!MVM.pingURL())
                {
                    //TODO add error: "You need to be connected to the internet to add online sources
                }
                else
                {
                    //Webscraper for metadata using HtmlAgilityPack
                    HtmlWeb web = new HtmlWeb();
                    HtmlDocument doc = web.Load(InputURL);

                    if (doc.ParsedText == null)
                    {
                        //TODO add error "invalid URL"
                        return;
                    }
                    HtmlNode src = doc.DocumentNode;

                    Codex newFile = new Codex(_data)
                    {
                        SourceURL = InputURL
                    };

                    IEnumerable<HtmlNode> pages;

                    switch (mode)
                    {
                        case ImportMode.GmBinder:
                            //Scrape metadata
                            newFile.Publisher = "GM Binder";
                            newFile.Title = src.SelectSingleNode("//html/head/title").InnerText.Split('|')[0];
                            newFile.Author = src.SelectSingleNode("//meta[@property='og:author']").GetAttributeValue("content", String.Empty);

                            //get pagecount
                            HtmlNode previewDiv = doc.GetElementbyId("preview");
                            pages = previewDiv.ChildNodes.Where(node => node.Id.Contains("p"));
                            newFile.PageCount = pages.Count();

                            //Get Cover Art
                            CoverArtGenerator.GetCoverFromURL(InputURL, newFile, ImportMode.GmBinder);

                            //add file to data
                            _data.AllFiles.Add(newFile);
                            RaisePropertyChanged("_data.AllFiles");
                            break;

                        case ImportMode.Homebrewery:
                            //Scrape metadata
                            newFile.Publisher = "Homebrewery";

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

                            //Get Cover Art
                            CoverArtGenerator.GetCoverFromURL(InputURL, newFile, ImportMode.Homebrewery);

                            //add file to data
                            _data.AllFiles.Add(newFile);
                            RaisePropertyChanged("_data.AllFiles");
                            break;
                    }                    
                }
            }
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
