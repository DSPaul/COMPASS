using FlashCap;
using System;
using System.Collections.Generic;
using System.Text;

namespace COMPASS.Common.Interfaces.Services
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
