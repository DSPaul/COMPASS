using COMPASS.Infra.Image;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Preferences;
using ImageMagick;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Metadata.Sources
{
    public class ImageMetadataSource : MetadataSource
    {
        private readonly IImageService _imageService;

        public ImageMetadataSource(ILogger logger, IPreferencesService preferencesService, IImageService imageService) :  
            base(logger, preferencesService) 
        {
            _imageService = imageService;
        }
        
        public override MetadataSourceType Type => MetadataSourceType.Image;

        public override bool IsValidSource(SourceSet sources) => File.Exists(sources.Path) && FileFormatUtils.IsImageFile(sources.Path);

        public override Task<SourceMetadata> GetMetadata(SourceSet sources, IList<Tag> availableTags, CancellationToken cancellationToken = default)
        {
            SourceMetadata metaData = new()
            {
                PageCount = 1
            };

            return Task.FromResult(metaData);
        }
        public override async Task<IMagickImage<byte>?> FetchCover(SourceSet sources, CancellationToken cancellationToken = default) 
            => await _imageService.LoadImageAsync(sources.Path, cancellationToken);
    }
}
