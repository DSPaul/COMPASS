using SkiaSharp;
namespace COMPASS.Infra.Avalonia.Barcode
{
    public interface IBarcodeDecoderService
    {
        /// <summary>
        /// Decodes a barcode from the given image and validates it using the provided validator function.
        /// </summary>
        /// <param name="image">The image containing the barcode to decode.</param>
        /// <param name="validator">A function to validate the decoded barcode.</param>
        /// <returns>The decoded barcode if valid; otherwise, null.</returns>
        string? Decode(SKBitmap image, Func<string, bool> validator);
    }
}
