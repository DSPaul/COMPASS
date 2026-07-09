using COMPASS.Common.Interfaces.Services;
using OpenCvSharp;
using SkiaSharp;

namespace COMPASS.Common.Services
{
    public class BarcodeDecoderService(ILogger logger) : IBarcodeDecoderService
    {
        public string? DecodeIsbn(SKBitmap frame)
        {
            try
            {
                using Mat mat = ConvertSkBitmapToMat(frame);

                using var detector = new BarcodeDetector();
                detector.DetectAndDecode(mat, out var points, out var results, out var types);
                return results?.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r) && ValidationService.IsValidISBN(r));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to decode ISBN in image", ex);
                return null;
            }
        }

        /// <summary>
        /// Safely maps SKBitmap memory to OpenCV Mat based on ColorType.
        /// OpenCV expects BGR or Grayscale.
        /// </summary>
        private Mat ConvertSkBitmapToMat(SKBitmap frame)
        {
            switch (frame.Info.ColorType)
            {
                case SKColorType.Bgra8888:
                    // BGRA maps perfectly to OpenCV's native format. Zero-copy mapping.
                    return Mat.FromPixelData(frame.Height, frame.Width, MatType.CV_8UC4, frame.GetPixels());

                case SKColorType.Rgba8888:
                    // OpenCV expects BGR, so we must convert RGBA.
                    using (var rgbaMat = Mat.FromPixelData(frame.Height, frame.Width, MatType.CV_8UC4, frame.GetPixels()))
                    {
                        var bgraMat = new Mat();
                        Cv2.CvtColor(rgbaMat, bgraMat, ColorConversionCodes.RGBA2BGRA);
                        return bgraMat;
                    }

                case SKColorType.Gray8:
                    // Grayscale maps perfectly. Zero-copy mapping.
                    return Mat.FromPixelData(frame.Height, frame.Width, MatType.CV_8UC1, frame.GetPixels());

                default:
                    throw new NotSupportedException($"Unhandled ColorType {frame.Info.ColorType}");
            }
        }
    }
}