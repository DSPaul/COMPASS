using SkiaSharp;

namespace COMPASS.Common.Interfaces.Services
{
    public interface IBarcodeDecoderService
    {
        string? DecodeIsbn(SKBitmap image);
    }
}
