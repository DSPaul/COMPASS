using CommunityToolkit.Mvvm.Input;
using COMPASS.Models;
using COMPASS.Models.Enums;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.Windows;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.ViewModels.Import
{
    public class ImportURLViewModel : ViewModelBase
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

        public ImportURLWindow? Window;

        private readonly ImportSource _importSource;

        //configuration props
        public string ImportTitle { get; } = "";
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

        private AsyncRelayCommand? _submitUrlCommand;
        public AsyncRelayCommand SubmitURLCommand => _submitUrlCommand ??= new(SubmitURL);
        public async Task SubmitURL()
        {
            if (!InputURL.Contains(ExampleURL) && ValidateURL)
            {
                ImportError = $"'{InputURL}' is not a valid URL for {ImportTitle}";
                return;
            }
            if (!IOService.PingURL())
            {
                ImportError = "You need to be connected to the internet to import an online source.";
                return;
            }
            Window?.Close();

            var progressVM = ProgressViewModel.GetInstance();
            progressVM.Log.Clear();
            progressVM.ResetCounter();
            progressVM.TotalAmount = 1;

            ProgressWindow progressWindow = new(3)
            {
                Owner = Application.Current.MainWindow
            };
            progressWindow.Show();

            Codex newCodex = await ImportURLAsync();

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

        private RelayCommand? _openBarcodeScannerCommand;
        public RelayCommand OpenBarcodeScannerCommand => _openBarcodeScannerCommand ??= new(OpenBarcodeScanner);
        private void OpenBarcodeScanner()
        {
            BarcodeScanWindow bcScanWindow = new();
            if (bcScanWindow.ShowDialog() == true)
            {
                InputURL = bcScanWindow.DecodedString;
            }
        }

        public async Task<Codex> ImportURLAsync()
        {
            var progressVM = ProgressViewModel.GetInstance();
            progressVM.ResetCounter();

            //Step 1: add codex
            progressVM.Text = "Adding new item to Collection";
            Codex newCodex = new(MainViewModel.CollectionVM.CurrentCollection);
            if (_importSource == ImportSource.ISBN)
            {
                newCodex.Sources.ISBN = InputURL;
            }
            else
            {
                newCodex.Sources.SourceURL = InputURL;
            }
            MainViewModel.CollectionVM.CurrentCollection.AllCodices.Add(newCodex);
            progressVM.IncrementCounter();
            progressVM.ResetCounter();

            // Steps 2: Scrape metadata
            progressVM.Text = "Downloading Metadata";

            try
            {
                await CodexViewModel.StartGetMetaDataProcess(newCodex)
                .ContinueWith(_ =>
                {
                    progressVM.AddLogEntry(new(Severity.Info, "Metadata loaded."));
                    progressVM.ResetCounter();
                });
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warn("Getting Metadata has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
                return newCodex;
            }

            // Step 3: Get Cover Art
            progressVM.Text = "Downloading Cover";
            try
            {
                await CoverService.GetCover(newCodex)
                 .ContinueWith(_ => progressVM.AddLogEntry(new(Severity.Info, "Cover loaded.")));
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warn("Getting covers/thumbnails has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
                return newCodex;
            }

            //Complete import
            string logMsg = $"Imported {newCodex.Title}";
            Logger.Info(logMsg);
            progressVM.AddLogEntry(new LogEntry(Severity.Info, logMsg));

            return newCodex;
        }
    }
}
