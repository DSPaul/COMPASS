using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using ImageMagick;

namespace COMPASS.Infra.Image
{
    internal class ImageService(ILogger logger) : IImageService
    {
        public IMagickImage<byte>? LoadImage(string imagePath)
        {
            //check if it's a valid file
            if (string.IsNullOrEmpty(imagePath) || !Path.Exists(imagePath))
            {
                return null;
            }

            if(!FileFormatUtils.IsImageFile(imagePath))
            {
                logger.Warn($"File {imagePath} is not a supported image file");
                return null;
            }

            try
            {
                return new MagickImage(imagePath);
            }
            catch (Exception ex)
            {
                //will fail if image is corrupt
                logger.Error($"Failed to load {imagePath}", ex);
                return null;
            }
        }

        public async Task<IMagickImage<byte>?> LoadImageAsync(string imagePath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => LoadImage(imagePath), cancellationToken);
        }
    }
}
