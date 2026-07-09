using COMPASS.Common.Interfaces.Services;
using FlashCap;

namespace COMPASS.Common.Services
{
    public class CameraService : ICameraService
    {
        public IEnumerable<CaptureDeviceDescriptor> ListCameras() 
            => new CaptureDevices().EnumerateDescriptors().Where(descr => descr.Characteristics.Any());

        public async Task<CaptureDevice> StartCapture(CaptureDeviceDescriptor deviceDescriptor, VideoCharacteristics characteristics, Action<PixelBuffer> onFrameCaptured)
        {
            var captureDevice = await deviceDescriptor.OpenAsync(characteristics, async bufferScope =>
            {
                onFrameCaptured?.Invoke(bufferScope.Buffer);
                bufferScope.ReleaseNow();
            });

            await captureDevice.StartAsync();
            return captureDevice;
        }
    }
}
