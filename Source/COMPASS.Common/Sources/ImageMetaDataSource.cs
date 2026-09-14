using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Infra.Tools.Logging;
using COMPASS.Infra.Tools;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public class ImageMetaDataSource : MetaDataSource
    {
        private readonly ICoverService _coverService;

        public ImageMetaDataSource(ILogger logger, IPreferencesService preferencesService, ICoverService coverService) :  
            base(logger, preferencesService) 
        {
            _coverService = coverService;
        }
        
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
            _coverService.GetCoverFromImage(sources.Path));
    }
}
