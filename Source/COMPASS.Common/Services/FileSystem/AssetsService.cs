using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using COMPASS.Common.Models;

namespace COMPASS.Common.Services.FileSystem
{
    public static class AssetsService
    {
        private static Bitmap? _noThumbnailPlaceholder;

        public static Bitmap NoThumbnailPlaceholder => _noThumbnailPlaceholder ??= new Bitmap(AssetLoader.Open(new Uri("avares://COMPASS.Common/Assets/CoverPlaceholder.png")));

        public static Bitmap GetPlaceholder(Codex codex) =>
            //for now only one placeholder, will have diffrent placeholders based on filetype/source in the future
            NoThumbnailPlaceholder;
        
        public static bool IsSharedAsset(Bitmap bitmap) => bitmap == NoThumbnailPlaceholder;
    }
}
