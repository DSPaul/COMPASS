using COMPASS.Models;
using COMPASS.Tools;
using System;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    class ImageSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.Image;

        public override async Task<bool> FetchCover(Codex codex) =>
            await Task.Run(() => CoverFetcher.GetCoverFromImage(codex.Path, codex));
        public override async Task<Codex> SetMetaData(Codex codex)
        {
            codex.PageCount = 1;
            return codex;
        }

        public override Codex SetTags(Codex codex) => throw new NotImplementedException();
    }
}
