using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using HtmlAgilityPack;
using System;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels.Sources
{
    public class DndBeyondSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.DnDBeyond;

        public override async Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            //Scrape metadata by going to store page, get to store page by using that /credits redirects there
            ProgressVM.AddLogEntry(new(Severity.Info, $"Connecting to DnD Beyond"));
            HtmlDocument? doc = await IOService.ScrapeSite(String.Concat(codex.SourceURL, "/credits"));
            HtmlNode? src = doc?.DocumentNode;

            if (src is null)
            {
                return codex;
            }

            //Set known metadata
            codex.Publisher = "D&D Beyond";
            codex.Authors = new() { "Wizards of the Coast" };

            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            if (String.IsNullOrEmpty(codex.SourceURL)) { return false; }
            try
            {
                //cover art is on store page, redirect there by going to /credits which every book has
                HtmlDocument? doc = await IOService.ScrapeSite(String.Concat(codex.SourceURL, "/credits"));
                HtmlNode? src = doc?.DocumentNode;
                if (src is null) return false;

                string imgURL = src.SelectSingleNode("//img[@class='product-hero-avatar__image']").GetAttributeValue("content", String.Empty);

                //download the file
                await CoverService.SaveCover(imgURL, codex);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get cover from DnDBeyond", ex);
                return false;
            }
        }

        public override bool IsValidSource(Codex codex) => false;
    }
}
