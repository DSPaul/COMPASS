using COMPASS.Infra.Avalonia.Barcode;
using SkiaSharp;

namespace COMPASS.Common.Features.ISBN;

public static class BarcodeDecoderExtensions
{
    extension(IBarcodeDecoderService decoder)
    {
        public string? DecodeISBN(SKBitmap frame) => decoder.Decode(frame, ISBNValidator.IsValidISBN);
    }
}
