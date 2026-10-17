using Autofac.Features.Indexed;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Metadata;
using COMPASS.Common.Features.Metadata.Sources;
using COMPASS.Infra.Avalonia.Barcode;
using COMPASS.Infra.Avalonia.Camera;
using COMPASS.Infra.Avalonia.Modal;
using COMPASS.Infra.DependencyInjection;
using System.Collections.ObjectModel;
using COMPASS.Common.Domain;
using COMPASS.Infra.Avalonia.Validation;
using COMPASS.Common.Features.Tabs;
namespace COMPASS.Common.Features.ISBN
{
    public class ISBNScannerViewModel : ValidatableViewModelBase, IModalViewModel, IConfirmable, IAsyncDisposable
    {
        private readonly DispatcherTimer _scanTimer;
        private readonly IBarcodeDecoderService _barcodeDecoderService;
        private readonly VideoCaptureViewModelFactory _videoCaptureVmFactory;
        private readonly CodexCollectionOperations _codexCollectionOperations;
        private readonly IActiveTabContext _activeTabContext;
        private readonly MetadataSource _isbnSource;
        private bool _scanning;

        public ISBNScannerViewModel(IBarcodeDecoderService barcodeDecoderService, VideoCaptureViewModelFactory videoCaptureVmFactory, CodexCollectionOperations codexCollectionOperations, IIndex<string, MetadataSource> metaDataSources,
            IActiveTabContext activeTabContext)
        {
            _activeTabContext = activeTabContext;
            _barcodeDecoderService = barcodeDecoderService;
            _videoCaptureVmFactory = videoCaptureVmFactory;
            _codexCollectionOperations = codexCollectionOperations;
            _isbnSource = metaDataSources[MetadataSourceType.ISBN.ToString()];
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
            if (!ISBNValidator.IsValidISBN(digits))
                AddError(nameof(Input), "Invalid ISBN.");
        }

        public RelayCommand<string> AddISBNCommand => field ??= new(AddISBN, CanAddISBN);
        private void AddISBN(string? isbn)
        {
            string? digits = isbn?.Replace("-", "").Replace(" ", "");
            if (ScannedCodes.Any(code => code.ISBN == digits)) return;
            if (!ISBNValidator.IsValidISBN(digits)) return;
            var scannedISBN = new ScannedISBN(_isbnSource, digits, _activeTabContext.ActiveCollection);
            ScannedCodes.Add(scannedISBN);
        }
        private bool CanAddISBN(string? isbn)
        {
            string? digits = isbn?.Replace("-", "").Replace(" ", "");
            return ISBNValidator.IsValidISBN(digits);
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
            VideoCaptureViewModel ??= _videoCaptureVmFactory.Create();
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

                    using var frameCopy = frame.Copy(); // safe snapshot � decouples decode from CurrentFrame's lifecycle
                    if (frameCopy == null) return;

                    var decoded = await Task.Run(() => _barcodeDecoderService.DecodeISBN(frameCopy));
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

            await _codexCollectionOperations.CreateCodicesAsync(sourceSets);
        }

        public async ValueTask DisposeAsync()
        {
            await StopScanning();
        }

        public class ScannedISBN : ObservableObject
        {
            public ScannedISBN(MetadataSource isbnSource, string isbn, CodexCollection targetCollection)
            {
                ISBN = isbn;
                Title = "Searching...";

                isbnSource.GetMetadata(new SourceSet()
                {
                    ISBN = isbn
                }, targetCollection.AllTags).ContinueWith(t =>
                {
                    Title = t.IsCompletedSuccessfully && !string.IsNullOrEmpty(t.Result.Title) ? t.Result.Title : "Unknown";
                });
            }

            public string ISBN { get; }

            public string Title { get; set => SetProperty(ref field, value); } = "";
        }
    }

    [Factory]
    public class ISBNScannerViewModelFactory(
        IBarcodeDecoderService barcodeDecoderService, 
        VideoCaptureViewModelFactory videoCaptureVmFactory, 
        CodexCollectionOperations codexCollectionOperations, 
        IIndex<string, MetadataSource> metaDataSources,
        IActiveTabContext activeTabContext)
    {
        public ISBNScannerViewModel Create() => new(barcodeDecoderService, videoCaptureVmFactory, codexCollectionOperations, metaDataSources, activeTabContext);
    }

}
