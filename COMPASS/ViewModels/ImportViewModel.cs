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

            //Call Relevant funcion
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
                    ImportURL(mode);
                    break;
                case ImportMode.Homebrewery:
                    ImportURL(mode);
                    break;
            }  
        }


        #region Properties
        readonly MainViewModel MVM;

        private Data _data;

        private float _progress;
        public float Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }

        private readonly string _importText = "Import in Progress: {0} out of {1}";
        private int _importcounter;
        private int _importamount;
        public string ProgressText
        {
            get { return string.Format(_importText,_importcounter,_importamount); }
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

        #endregion

        #region Functions and Events

        private void ImportFromPdf(object sender, DoWorkEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AddExtension = false,
                Multiselect = true
            };

            Codex SelectWhenDone = null;

            if (openFileDialog.ShowDialog() == true)
            {
                ProgressWindow pgw;

                //needed to avoid error "The calling Thread must be STA" when creating progress window
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        pgw = new ProgressWindow(this);
                        pgw.Show();
                    });
                int progcounter = 1;
                foreach (string path in openFileDialog.FileNames)
                {
                    //Update Progress Bar
                    float prog = (float)progcounter / openFileDialog.FileNames.Length  * 100;               
                    (sender as BackgroundWorker).ReportProgress((int)prog, new Tuple<int, int>(progcounter, openFileDialog.FileNames.Length));
                    progcounter++;

                    //Import File
                    if (_data.AllFiles.All(p => p.Path != path))
                    {
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

        private void ImportURL(ImportMode mode)
        {
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
            Progress = e.ProgressPercentage;
            var args = (Tuple<int, int>)e.UserState;
            _importcounter = args.Item1;
            _importamount = args.Item2;
            RaisePropertyChanged("ImportText");
        }
        #endregion
    }
}
