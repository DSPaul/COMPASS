using FlashCap;
namespace COMPASS.Infra.Avalonia.Camera
{
    public interface ICameraService
    {
        /// <summary>
        /// List available cameras
        /// </summary>
        /// <returns></returns>
        IEnumerable<CaptureDeviceDescriptor> ListCameras();

        Task<CaptureDevice> StartCapture(CaptureDeviceDescriptor deviceDescriptor, VideoCharacteristics characteristics, Action<PixelBuffer> onFrameCaptured);
    }
}
