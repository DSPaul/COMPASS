using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Infra.Tools;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public class ImageMetaDataSource : MetaDataSource
    {
        public ImageMetaDataSource(ILogger logger, IPreferencesService preferencesService) :  
            base(logger, preferencesService) { }
        
        public override MetaDataSourceType Type => MetaDataSourceType.Image;

        public override bool IsValidSource(SourceSet sources) => File.Exists(sources.Path) && FileFormatUtils.IsImageFile(sources.Path);

        public override Task<SourceMetaData> GetMetaData(SourceSet sources, IList<Tag> availableTags)
        {
            SourceMetaData metaData = new()
            {
                PageCount = 1
            };

            return Task.FromResult(metaData);
        }
        public override async Task<IMagickImage<byte>?> FetchCover(SourceSet sources) => await Task.Run(() => 
            ServiceResolver.Resolve<CoverService>().GetCoverFromImage(sources.Path));
    }
}
