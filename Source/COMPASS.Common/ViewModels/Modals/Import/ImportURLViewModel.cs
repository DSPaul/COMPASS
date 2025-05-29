using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Modals.Edit;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.ViewModels.Modals.Import
{
    public class ImportURLViewModel : ViewModelBase, IModalViewModel, IConfirmable
    {

        public ImportURLViewModel(ImportSource importSource)
        {
            _importSource = importSource;
            switch (importSource)
            {
                case ImportSource.GmBinder:
                    SourceName = "GM Binder";
                    ExampleURL = "https://www.gmbinder.com/share/";
                    break;
                case ImportSource.Homebrewery:
                    SourceName = "Homebrewery";
                    ExampleURL = "https://homebrewery.naturalcrit.com/share/";
                    ShowValidateDisableCheckbox = true;
                    break;
                case ImportSource.GoogleDrive:
                    SourceName = "Google Drive";
                    ExampleURL = "https://drive.google.com/file/";
                    break;
                case ImportSource.GenericURL:
                    SourceName = "Any URL";
                    ExampleURL = "https://";
                    break;
                case ImportSource.ISBN:
                    SourceName = "ISBN";
                    ExampleURL = "";
                    ShowScannerButton = true;
                    break;
            }
        }

        private readonly ImportSource _importSource;

        //configuration props
        public string SourceName { get; } = "";
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
        
        private AsyncRelayCommand? _openBarcodeScannerCommand;
        public AsyncRelayCommand OpenBarcodeScannerCommand => _openBarcodeScannerCommand ??= new(OpenBarcodeScanner);
        private async Task OpenBarcodeScanner()
        {
            BarcodeScanWindow bcScanWindow = new();
            await bcScanWindow.ShowDialog(App.MainWindow);
            if (!string.IsNullOrEmpty(bcScanWindow.DecodedString))
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
            Codex newCodex = CodexOperations.CreateNewCodex(MainViewModel.CollectionVM.CurrentCollection);
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
                await CodexOperations.StartGetMetaDataProcess(newCodex)
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

        #region IConfirmable
        
        private RelayCommand? _cancelCommand;
        public IRelayCommand CancelCommand => _cancelCommand ??= new(CloseAction);
        
        private AsyncRelayCommand? _submitUrlCommand;
        public IRelayCommand ConfirmCommand => _submitUrlCommand ??= new (SubmitURL);
        private async Task SubmitURL()
        {
            if (!InputURL.Contains(ExampleURL) && ValidateURL)
            {
                ImportError = $"'{InputURL}' is not a valid URL for {SourceName}";
                return;
            }
            if (!IOService.PingURL())
            {
                ImportError = "You need to be connected to the internet to import an online source.";
                return;
            }

            CloseAction();

            var progressVM = ProgressViewModel.GetInstance();
            progressVM.Log.Clear();
            progressVM.ResetCounter();
            progressVM.TotalAmount = 1;
            
            //TODO uncomment when implemented
            // ProgressWindow progressWindow = new(3);
            // progressWindow.Show(App.MainWindow);

            Codex newCodex = await ImportURLAsync();

            if (ShowEditWhenDone)
            {
                CodexEditViewModel vm = new(newCodex);
                ModalWindow editWindow = new(vm);
                await editWindow.ShowDialog(App.MainWindow);
            }
        }

        
        #endregion
        
        #region IModelViewModel

        public string WindowTitle => $"Import an item from {SourceName}";
        public int? WindowWidth => 600;
        public int? WindowHeight => null;
        public Action CloseAction { get; set; } = () => { };

        #endregion
    }
}
