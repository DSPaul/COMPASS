using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels.Sources
{
    public class GoogleDriveSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.GoogleDrive;
        public override bool IsValidSource(SourceSet soures) =>
            soures.HasOnlineSource() && soures.SourceURL.Contains(new ImportURLViewModel(ImportSource.GoogleDrive).ExampleURL);

        public override Task<CodexDto> GetMetaData(SourceSet sources)
        {
            Debug.Assert(IsValidSource(sources), "Invalid Codex was used in Google drive source");

            // Use a codex dto to tranfer the data
            CodexDto codex = new()
            {
                Publisher = "Google Drive"
            };

            return Task.FromResult(codex);
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            if (String.IsNullOrEmpty(codex.Sources.SourceURL)) { return false; }
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading cover from Google Drive"));
            try
            {
                //cover art is on store page, redirect there by going to /credits which every book has
                HtmlDocument? doc = await IOService.ScrapeSite(codex.Sources.SourceURL);
                HtmlNode? src = doc?.DocumentNode;
                if (src is null) return false;

                string imgURL = src.SelectSingleNode("//meta[@property='og:image']").GetAttributeValue("content", String.Empty);
                //cut of "=W***-h***-p" from URL that crops the image if it is present
                if (imgURL.Contains('=')) imgURL = imgURL.Split('=')[0];
                await CoverService.SaveCover(imgURL, codex);
                return true;
            }
            catch (Exception ex)
            {
                string msg = $"Failed to get cover from {codex.Sources.SourceURL}";
                Logger.Error(msg, ex);
                ProgressVM.AddLogEntry(new(Severity.Error, msg));
                return false;
            }
        }

    }
}
