using COMPASS.Models;
using COMPASS.Tools;
using System.IO;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    class ImageSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.Image;

        public override bool IsValidSource(Codex codex) => File.Exists(codex.Path) && Utils.IsImageFile(codex.Path);

        public override async Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            codex.PageCount = 1;
            return codex;
        }
        public override async Task<bool> FetchCover(Codex codex) =>
            await Task.Run(() => CoverFetcher.GetCoverFromImage(codex.Path, codex));
    }
}
