using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public class ImageMetaDataSource : MetaDataSource
    {
        public override MetaDataSourceType Type => MetaDataSourceType.Image;

        public override bool IsValidSource(SourceSet sources) => File.Exists(sources.Path) && IOService.IsImageFile(sources.Path);

        public override Task<SourceMetaData> GetMetaData(SourceSet sources)
        {
            SourceMetaData metaData = new()
            {
                PageCount = 1
            };

            return Task.FromResult(metaData);
        }
        public override async Task<IMagickImage?> FetchCover(SourceSet sources) => await Task.Run(() => 
            CoverService.GetCoverFromImage(sources.Path));
    }
}
