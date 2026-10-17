using COMPASS.Infra.Collections;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Preferences;
using ImageMagick;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Metadata.Sources
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
