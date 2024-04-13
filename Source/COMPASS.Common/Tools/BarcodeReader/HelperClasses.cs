using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace COMPASS.Common.Tools.BarcodeReader
{
    // based on https://github.com/FrancescoBonizzi/WebcamControl-WPF-With-OpenCV
    public static class BitmapExtensions
    {
        public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }
    }

    public class QRCodeReadEventArgs : EventArgs
    {
        public QRCodeReadEventArgs(string? qRCodeData)
        {
            QRCodeData = qRCodeData;
        }

        public string? QRCodeData { get; }
    }
}
