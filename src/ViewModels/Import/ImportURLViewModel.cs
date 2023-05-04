using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.ViewModels.Import
{
    public class ImportURLViewModel : ObservableObject
    {

        public ImportURLViewModel(ImportSource importSource)
        {
            _importSource = importSource;
            switch (importSource)
            {
                case ImportSource.GmBinder:
                    ImportTitle = "GM Binder";
                    ExampleURL = "https://www.gmbinder.com/share/";
                    break;
                case ImportSource.Homebrewery:
                    ImportTitle = "Homebrewery";
                    ExampleURL = "https://homebrewery.naturalcrit.com/share/";
                    ShowValidateDisableCheckbox = true;
                    break;
                case ImportSource.GoogleDrive:
                    ImportTitle = "Google Drive";
                    ExampleURL = "https://drive.google.com/file/";
                    break;
                case ImportSource.GenericURL:
                    ImportTitle = "Any URL";
                    ExampleURL = "https://";
                    break;
                case ImportSource.ISBN:
                    ImportTitle = "ISBN";
                    ExampleURL = "";
                    ShowScannerButton = true;
                    break;
            }
        }

        protected ImportURLWindow importURLwindow;

        private ImportSource _importSource;

        //configuration props
        public string ImportTitle { get; init; } = "";
        public string ExampleURL { get; init; } = "";
        public bool ShowValidateDisableCheckbox { get; init; } = false;
        public bool ShowScannerButton { get; init; } = false;

        //State props
        public bool ValidateURL { get; set; } = true;
        public bool ShowEditWhenDone { get; set; } = false;

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
                ImportError = "You need to be connected to the internet to import an online source.";
                return;
            }
            importURLwindow.Close();

            var ProgressVM = ProgressViewModel.GetInstance();

            ProgressVM.ResetCounter();
            ProgressVM.Text = "Importing, Step: ";
            //3 steps: 1. connect to site, 2. get metadata, 3. get Cover
            ProgressVM.TotalAmount = 3;

            ProgressWindow progressWindow = new()
            {
                Owner = Application.Current.MainWindow
            };
            progressWindow.Show();

            Task.Run(ImportURL);
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

        public async void ImportURL()
        {
            Codex newCodex = new(MainViewModel.CollectionVM.CurrentCollection);
            if (_importSource == ImportSource.ISBN)
            {
                newCodex.ISBN = InputURL;
            }
            else
            {
                newCodex.SourceURL = InputURL;
            }

            // Steps 1 & 2: Load Source and Scrape metadata
            await CodexViewModel.GetMetaData(newCodex);

            var ProgressVM = ProgressViewModel.GetInstance();
            ProgressVM.IncrementCounter();
            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, "Metadata loaded. Downloading cover art."));

            // Step 3: Get Cover Art
            await CoverFetcher.GetCover(newCodex);
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
    }
}
