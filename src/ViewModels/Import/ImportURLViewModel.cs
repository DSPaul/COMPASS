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

        public ImportURLWindow Window;

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
        public async void SubmitURL()
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
            Window.Close();

            var ProgressVM = ProgressViewModel.GetInstance();
            ProgressVM.Log.Clear();
            ProgressVM.ResetCounter();
            ProgressVM.TotalAmount = 1;

            ProgressWindow progressWindow = new(3)
            {
                Owner = Application.Current.MainWindow
            };
            progressWindow.Show();

            Codex newCodex = await Task.Run(ImportURL);

            if (ShowEditWhenDone)
            {
                CodexEditWindow editWindow = new(new CodexEditViewModel(newCodex))
                {
                    Topmost = true,
                    Owner = Application.Current.MainWindow
                };
                editWindow.ShowDialog();
            }
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

        public async Task<Codex> ImportURL()
        {
            var ProgressVM = ProgressViewModel.GetInstance();
            ProgressVM.ResetCounter();

            //Step 1: add codex
            ProgressVM.Text = "Adding new item to Collection";
            Codex newCodex = new(MainViewModel.CollectionVM.CurrentCollection);
            if (_importSource == ImportSource.ISBN)
            {
                newCodex.ISBN = InputURL;
            }
            else
            {
                newCodex.SourceURL = InputURL;
            }
            MainViewModel.CollectionVM.CurrentCollection.AllCodices.Add(newCodex);
            ProgressVM.IncrementCounter();
            ProgressVM.ResetCounter();

            // Steps 2: Scrape metadata
            ProgressVM.Text = "Downloading Metadata";
            await CodexViewModel.StartGetMetaDataProcess(newCodex)
                .ContinueWith(_ =>
                {
                    ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, "Metadata loaded."));
                    ProgressVM.ResetCounter();
                });

            // Step 3: Get Cover Art

            ProgressVM.Text = "Downloading Cover";
            await CoverFetcher.GetCover(newCodex)
                 .ContinueWith(_ =>
                 {
                     ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, "Cover loaded."));
                     ProgressVM.ResetCounter();
                 });

            //Complete import
            string logMsg = $"Imported {newCodex.Title}";
            Logger.Info(logMsg);
            ProgressVM.AddLogEntry(new LogEntry(LogEntry.MsgType.Info, logMsg));

            return newCodex;
        }
    }
}
