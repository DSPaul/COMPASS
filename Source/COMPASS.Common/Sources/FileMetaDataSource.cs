using System.IO;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public class FileMetaDataSource : MetaDataSource
    {
        public override MetaDataSourceType Type => MetaDataSourceType.File;

        public override Task<IMagickImage?> FetchCover(SourceSet sources) => throw new System.NotImplementedException();
        public override bool IsValidSource(SourceSet sources) => sources.HasOfflineSource();

        public override Task<SourceMetaData> GetMetaData(SourceSet sources)
        {
            SourceMetaData metaData = new()
            {
                Title = Path.GetFileNameWithoutExtension(sources.Path)
            };

            // Tags based on file path
            foreach (Tag tag in TargetCollection.AllTags)
            {
                var globs = tag.LinkedGlobs.Concat(tag.CalculatedLinkedGlobs).ToList();
                if (IOService.MatchesAnyGlob(sources.Path, globs))
                {
                    metaData.Tags.AddIfMissing(tag);
                }
            }

            MainViewModel.CollectionVM.FilterVM.ReFilter();
            return Task.FromResult(metaData);
        }
    }
}
