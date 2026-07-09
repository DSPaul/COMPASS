using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.Sources;
using COMPASS.Common.ViewModels.Components;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Tools;
using System.Collections.ObjectModel;

namespace COMPASS.Common.ViewModels.Modals.Import
{
    public class ISBNScannerViewModel : ViewModelBase, IModalViewModel, IConfirmable, IAsyncDisposable
    {
        private readonly DispatcherTimer _scanTimer;
        private readonly IBarcodeDecoderService _barcodeDecoderService;
        private bool _scanning;

        public ISBNScannerViewModel()
        {
            _barcodeDecoderService = ServiceResolver.Resolve<IBarcodeDecoderService>();
            _scanTimer = new(TimeSpan.FromMilliseconds(50), DispatcherPriority.Render, OnScanTick);

            ScannedCodes = [];
        }

        public VideoCaptureViewModel? VideoCaptureViewModel { get; private set => SetProperty(ref field, value); }
        public ObservableCollection<ScannedISBN> ScannedCodes { get; }
        public bool ShowWebcam { get; set => SetProperty(ref field, value); }

        public string Input 
        { 
            get; 
            set 
            {
                SetProperty(ref field, value);
                ValidateInput();
            } 
        } = "";

        private void ValidateInput()
        {
            ClearErrors(nameof(Input));
            if (string.IsNullOrWhiteSpace(Input))
            {
                AddError(nameof(Input), "ISBN is required.");
                return;
            }
            string digits = Input.Replace("-", "").Replace(" ", "");
            if (!ValidationService.IsValidISBN(digits))
                AddError(nameof(Input), "Invalid ISBN.");
        }

        public RelayCommand<string> AddISBNCommand => field ??= new(AddISBN, CanAddISBN);
        private void AddISBN(string? isbn)
        {
            string? digits = isbn?.Replace("-", "").Replace(" ", "");
            if (ScannedCodes.Any(code => code.ISBN == digits)) return;
            if (!ValidationService.IsValidISBN(digits)) return;
            var scannedISBN = new ScannedISBN(digits);
            ScannedCodes.Add(scannedISBN);
        }
        private bool CanAddISBN(string? isbn)
        {
            string? digits = isbn?.Replace("-", "").Replace(" ", "");
            return ValidationService.IsValidISBN(digits);
        }

        public RelayCommand<ScannedISBN> RemoveCommand => field ??= new(RemoveScannedCode);
        private void RemoveScannedCode(ScannedISBN? scannedCode)
        {
            if (scannedCode != null)
            {
                ScannedCodes.Remove(scannedCode);
            }
        }

        public RelayCommand StartScanningCommand => field ??= new(StartScanning);
        private void StartScanning()
        {
            ShowWebcam = true;
            VideoCaptureViewModel ??= new VideoCaptureViewModel();
            _scanTimer.Start();
        }

        public AsyncRelayCommand StopScanningCommand => field ??= new(StopScanning);
        private async Task StopScanning()
        {
            ShowWebcam = false;
            _scanTimer.Stop();

            if(VideoCaptureViewModel != null)
            {
                await VideoCaptureViewModel.DisposeAsync();
            }
        }

        #region IModalViewModel
        public string WindowTitle => "Scan ISBN barcodes";

        public Action CloseAction { get; set; } = () => { };
        #endregion

        #region IConfirmable
        public IRelayCommand CancelCommand => field ??= new RelayCommand(CloseAction);

        public IRelayCommand ConfirmCommand => field ??= new AsyncRelayCommand(SubmitCodices);
        #endregion

        private async void OnScanTick(object? sender, EventArgs e)
        {
            if (!_scanning)
            {
                _scanning = true;
                try
                {
                    var frame = VideoCaptureViewModel?.CurrentFrame;
                    if (frame == null) return;

                    using var frameCopy = frame.Copy(); // safe snapshot — decouples decode from CurrentFrame's lifecycle
                    if (frameCopy == null) return;

                    var decoded = await Task.Run(() => _barcodeDecoderService.DecodeIsbn(frameCopy));
                    if (!string.IsNullOrEmpty(decoded))
                    {
                        AddISBN(decoded);
                    }
                }
                finally
                {
                    _scanning = false;
                }
            }
        }

        private async Task SubmitCodices()
        {
            CloseAction();
            var sourceSets = ScannedCodes.Select(code => new SourceSet()
            {
                ISBN = code.ISBN
            }).ToList();

           await ImportViewModel.CreateCodicesAsync(sourceSets);
        }

        public async ValueTask DisposeAsync()
        {
            await StopScanning();
        }

        public class ScannedISBN : ObservableObject
        {
            public ScannedISBN(string isbn)
            {
                ISBN = isbn;
                Title = "Searching...";

                var activeCollection = TabsViewModel.GetInstance().ActiveTab!.CollectionVM.Collection;
                isbnSource = new ISBNMetaDataSource(activeCollection);
                isbnSource.GetMetaData(new SourceSet()
                {
                    ISBN = isbn
                }).ContinueWith(t =>
                {
                    Title = t.IsCompletedSuccessfully && !string.IsNullOrEmpty(t.Result.Title) ? t.Result.Title : "Unknown";
                });
            }

            private ISBNMetaDataSource isbnSource;

            public string ISBN { get; }

            public string Title { get; set => SetProperty(ref field, value); } = "";
        }
    }

}
