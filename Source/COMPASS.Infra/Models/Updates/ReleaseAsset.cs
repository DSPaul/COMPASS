using System;
using System.Collections.Generic;
using System.Text;

namespace COMPASS.Infra.Models.Updates
{
    public struct ReleaseAsset
    {
        public ReleaseAsset(string assetName, string downloadUrl, string checkSum, long sizeInBytes)
        {
            AssetName = assetName;
            DownloadUrl = downloadUrl;
            Checksum = checkSum;
            SizeInBytes = sizeInBytes;
        }

        public string AssetName { get; }

        public string DownloadUrl { get; }

        public string Checksum { get; } 

        public long SizeInBytes { get; }
    }
}
