using COMPASS.Models;
using COMPASS.Services;
using COMPASS.Tools;
using System.IO;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    class ImageSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.Image;

        public override bool IsValidSource(Codex codex) => File.Exists(codex.Path) && IOService.IsImageFile(codex.Path);

        public override Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex)
            {
                PageCount = 1
            };

            return Task.FromResult(codex);
        }
        public override async Task<bool> FetchCover(Codex codex) =>
            await Task.Run(() => CoverService.GetCoverFromImage(codex.Path, codex));
    }
}
