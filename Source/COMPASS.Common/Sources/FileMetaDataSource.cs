using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools.Logging;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public class FileMetadataSource : MetadataSource
    {
        public FileMetadataSource(ILogger logger, IPreferencesService preferencesService) :
            base(logger, preferencesService) { }
        public override MetadataSourceType Type => MetadataSourceType.File;

        public override Task<IMagickImage<byte>?> FetchCover(SourceSet sources, CancellationToken cancellationToken = default) => throw new System.NotImplementedException();
        public override bool IsValidSource(SourceSet sources) => sources.HasOfflineSource();

        public override Task<SourceMetadata> GetMetadata(SourceSet sources, IList<Tag> availableTags, CancellationToken cancellationToken = default)
        {
            SourceMetadata metaData = new()
            {
                Title = Path.GetFileNameWithoutExtension(sources.Path)
            };

            foreach (var tag in GetMatchingTags(sources, availableTags))
            { 
                metaData.Tags.AddIfMissing(tag);
            }
             
            return Task.FromResult(metaData);
        }
    }
}
