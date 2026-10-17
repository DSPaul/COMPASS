using Avalonia.Media.Imaging;
using Avalonia.Platform;
using COMPASS.Infra.IO;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Covers
{
    public static class AssetsService
    {

        public static Bitmap DocumentNoThumbnailPlaceholder => field ??= new Bitmap(AssetLoader.Open(new Uri("avares://COMPASS.Common/Assets/CoverPlaceholder_Document.png")));
        public static Bitmap WebNoThumbnailPlaceholder => field ??= new Bitmap(AssetLoader.Open(new Uri("avares://COMPASS.Common/Assets/CoverPlaceholder_Web.png")));
        public static Bitmap GenericNoThumbnailPlaceholder => field ??= new Bitmap(AssetLoader.Open(new Uri("avares://COMPASS.Common/Assets/CoverPlaceholder.png")));
        public static Bitmap GetPlaceholder(Codex codex)
        {
            bool isBook =
                !string.IsNullOrEmpty(codex.Sources.ISBN) ||
                codex.PhysicallyOwned ||
                FileFormatUtils.IsPDFFile(codex.Sources.FileName);

            if(isBook)
            {
                return DocumentNoThumbnailPlaceholder;
            }
            else if (!string.IsNullOrEmpty(codex.Sources.SourceURL))
            {
                return WebNoThumbnailPlaceholder;
            }
            else
            {
                return GenericNoThumbnailPlaceholder;
            }
        }
        
        public static bool IsSharedAsset(Bitmap bitmap) => 
            bitmap == GenericNoThumbnailPlaceholder || 
            bitmap == WebNoThumbnailPlaceholder || 
            bitmap == DocumentNoThumbnailPlaceholder;
    }
}
