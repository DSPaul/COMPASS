using System;
using System.Diagnostics;
using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Modals.Import;
using HtmlAgilityPack;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public class GoogleDriveMetaDataSource : MetaDataSource
    {
        public override MetaDataSourceType Type => MetaDataSourceType.GoogleDrive;
        public override bool IsValidSource(SourceSet soures) =>
            soures.HasOnlineSource() && soures.SourceURL.Contains(new ImportURLViewModel(ImportSource.GoogleDrive).ExampleURL);

        public override Task<SourceMetaData> GetMetaData(SourceSet sources)
        {
            Debug.Assert(IsValidSource(sources), "Invalid Codex was used in Google drive source");
            
            SourceMetaData metaData = new()
            {
                Publisher = "Google Drive"
            };

            return Task.FromResult(metaData);
        }

        public override async Task<IMagickImage?> FetchCover(SourceSet sources)
        {
            if (String.IsNullOrEmpty(sources.SourceURL)) { return null; }
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading cover from Google Drive"));
            try
            {
                //cover art is on store page, redirect there by going to /credits which every book has
                HtmlDocument? doc = await IOService.ScrapeSite(sources.SourceURL);
                HtmlNode? src = doc?.DocumentNode;
                if (src is null) return null;

                string imgURL = "";
                if (src.SelectSingleNode("//meta[@property='og:image']") is HtmlNode imgNode)
                {
                    imgURL = imgNode.GetAttributeValue("content", String.Empty);
                    //cut of "=W***-h***-p" from URL that crops the image if it is present
                    if (imgURL.Contains('=')) imgURL = imgURL.Split('=')[0];
                }

                else if (src.SelectSingleNode("//link[@id='texmex-thumb']") is HtmlNode linkNode)
                {
                    imgURL = linkNode.GetAttributeValue("href", string.Empty);
                }

                if (!string.IsNullOrEmpty(imgURL))
                {
                    return await IOService.DownloadImageAsync(imgURL);
                }
            }
            catch (Exception ex)
            {
                string msg = $"Failed to get cover from {sources.SourceURL}";
                Logger.Error(msg, ex);
                ProgressVM.AddLogEntry(new(Severity.Error, msg));
            }

            return null;
        }
    }
}
