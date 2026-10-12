using ImageMagick;

namespace COMPASS.Infra.Image
{
    public interface IImageService
    {
        IMagickImage<byte>? LoadImage(string imagePath);
        Task<IMagickImage<byte>?> LoadImageAsync(string imagePath, CancellationToken cancellationToken = default);
    }
}
