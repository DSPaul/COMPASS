using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Tools;
using FlashCap;
using SkiaSharp;

namespace COMPASS.Common.ViewModels.Components
{
    public class VideoCaptureViewModel : ViewModelBase, IAsyncDisposable
    {
        private ICameraService _cameraService = ServiceResolver.Resolve<ICameraService>();
        private CaptureDevice? _activeDevice;

        public VideoCaptureViewModel()
        {
            DeviceDescriptors = [];
            Characteristics = [];
            RefreshCameras();
        }

        #region Properties

        public RangeObservableCollection<CaptureDeviceDescriptor> DeviceDescriptors { get; }
        public RangeObservableCollection<VideoCharacteristics> Characteristics { get; }

        public CaptureDeviceDescriptor? SelectedDescriptor
        {
            get;
            set
            {
                if(SetProperty(ref field, value))
                {
                    //Fire and forget
                    _ = OnDescriptorChanged();
                }
            }
        }
        public VideoCharacteristics? SelectedCharacteristic 
        { 
            get;
            set
            {
                if(SetProperty(ref field, value))
                {
                    //Fire and forget
                    _ = OnCharacteristicChanged();
                }
            }
        }

        public SKBitmap? CurrentFrame
        {
            get => field;
            set
            {
                field?.Dispose();
                field = null;
                SetProperty(ref field, value);
            }
        }

        #endregion

        public RelayCommand RefreshCamerasCommand => field ??= new RelayCommand(RefreshCameras);
        private void RefreshCameras()
        {
            CaptureDeviceDescriptor? prevSelected = SelectedDescriptor;

            DeviceDescriptors.Clear();
            foreach (var camera in _cameraService.ListCameras())
            {
                DeviceDescriptors.Add(camera);
            }

            if(prevSelected != null && DeviceDescriptors.Contains(prevSelected))
            {
                SelectedDescriptor = prevSelected;
            }
            else
            {
                SelectedDescriptor = DeviceDescriptors.FirstOrDefault();
            }
        }

        private async Task OnDescriptorChanged()
        {
            await StopCapture();

            Characteristics.Clear();
            var characteristics = (SelectedDescriptor?.Characteristics ?? [])
                .Where(c => c.PixelFormat != PixelFormats.Unknown);
            Characteristics.AddRange(characteristics);
            SelectedCharacteristic = characteristics.FirstOrDefault();
        }

        private async Task OnCharacteristicChanged()
        {
            await StopCapture();

            if (SelectedDescriptor != null && SelectedCharacteristic != null)
            {
                _activeDevice = await _cameraService.StartCapture(SelectedDescriptor, SelectedCharacteristic, OnFrameCaptured);
            }
        }

        private async Task StopCapture()
        {
            if (_activeDevice != null)
            {
                await _activeDevice.StopAsync();
                await _activeDevice.DisposeAsync();
            }
            _activeDevice = null;
        }

        private void OnFrameCaptured(PixelBuffer buffer)
        {
            ArraySegment<byte> image = buffer.ReferImage();

            //Update frame on UI thread
            Dispatcher.UIThread.Invoke(() => 
                CurrentFrame = SKBitmap.Decode(image)
            );
        }

        public async ValueTask DisposeAsync()
        {
            await StopCapture();
            CurrentFrame?.Dispose();
        }
    }
}
