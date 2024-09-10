using COMPASS.Common.Models;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Services;
using System.IO;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels.Sources
{
    class ImageSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.Image;

        public override bool IsValidSource(SourceSet sources) => File.Exists(sources.Path) && IOService.IsImageFile(sources.Path);

        public override Task<CodexDto> GetMetaData(SourceSet sources)
        {
            // Work on a copy
            CodexDto codex = new()
            {
                PageCount = 1
            };

            return Task.FromResult(codex);
        }
        public override async Task<bool> FetchCover(Codex codex) =>
            await Task.Run(() => CoverService.GetCoverFromImage(codex.Sources.Path, codex));
    }
}
