using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Sources;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using COMPASS.Common.Views.Windows;
using ImageMagick;
using ImageMagick.Factories;
using OpenQA.Selenium;

namespace COMPASS.Common.Services
{
    public static class CoverService
    {
        private const int ThumbnailWidth = 200;
        private const int CoverWidth = 850;
        
        /// <summary>
        /// Fetches a cover image for the given codex
        /// </summary>
        /// <param name="codex"></param>
        /// <param name="chooseMetaDataViewModel"></param>
        /// <exception cref="System.OperationCanceledException">The token has had cancellation requested.</exception>
        public static async Task GetAndApplyCover(Codex codex, ChooseMetaDataViewModel? chooseMetaDataViewModel = null)
        {
            IMagickImage? coverFromSource = null;
            try
            {
                CodexProperty coverProp = PreferencesService.GetInstance().Preferences.ImportableCodexProperties.First(prop => prop.Name == nameof(Codex.Cover));
                
                switch (coverProp.OverwriteMode)
                {
                    case MetaDataOverwriteMode.Ask:
                        Debug.Assert(chooseMetaDataViewModel is not null, "choose MetaData ViewModel cannot be null if overwrite mode is ask");
                        break;
                    case MetaDataOverwriteMode.Never:
                    case MetaDataOverwriteMode.IfEmpty when !coverProp.IsEmpty(codex):
                        return;
                }

                bool shouldAsk = coverProp.OverwriteMode == MetaDataOverwriteMode.Ask && !coverProp.IsEmpty(codex);

                
                foreach (var sourceType in coverProp.SourcePriority)
                {
                    ProgressViewModel.GlobalCancellationTokenSource.Token.ThrowIfCancellationRequested();

                    MetaDataSource? source = MetaDataSource.GetSource(sourceType);
                    if (source == null || !source.IsValidSource(codex.Sources)) continue;
                    coverFromSource = await source.FetchCover(codex.Sources);
                    if (coverFromSource != null) break;
                }

                //no cover was found
                if (coverFromSource == null) return;

                if (shouldAsk)
                {
                    SourceMetaData newMetaData = new()
                    {
                        Cover = coverFromSource,
                    };
                    
                    //check if the image is different from the existing one
                    if (!coverProp.HasNewValue(newMetaData, codex)) return;

                    //make a copy of cover because originals lifetime is limited to this method
                    newMetaData.Cover = new MagickImage((IMagickImage<byte>)coverFromSource); 
                    chooseMetaDataViewModel!.AddMetaDataProposal(codex, newMetaData);
                }
                else
                {
                    await SaveCover(codex, coverFromSource);
                }
            }
            finally
            {
                coverFromSource?.Dispose();
                ProgressViewModel.GetInstance().IncrementCounter();
            }
        }

        public static async Task GetAndApplyCover(List<Codex> codices)
        {
            var progressVM = ProgressViewModel.GetInstance();
            progressVM.ResetCounter();
            progressVM.TotalAmount = codices.Count;
            progressVM.Text = "Getting Cover";

            ChooseMetaDataViewModel chooseMetaDataVM = new();

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount / 2, 1)
            };

            try
            {
                await Parallel.ForEachAsync(codices, parallelOptions, async (codex, _) => await GetAndApplyCover(codex, chooseMetaDataVM));
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warn("Renewing covers has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
            }

            if (chooseMetaDataVM.MetaDataProposals.Any())
            {
                ChooseMetaDataWindow window = new(chooseMetaDataVM);
                window.Show();
            }
        }

        public static async Task SaveCover(Codex destCodex, IMagickImage image)
        {
            if (string.IsNullOrEmpty(destCodex.CoverArtPath))
            {
                Logger.Error("Trying to write cover img to empty path", new InvalidOperationException());
                return;
            }

            if (image.Width > CoverWidth) image.Resize(CoverWidth, 0);

            if (IOService.EnsureDirectoryExists(destCodex.CoverArtPath))
            {
                await image.WriteAsync(destCodex.CoverArtPath);
                CreateThumbnail(destCodex, image);
                destCodex.RefreshThumbnail();
            }
        }

        public static MagickImage? GetCoverFromImage(string imagePath)
        {
            //check if it's a valid file
            if (string.IsNullOrEmpty(imagePath) ||
                !Path.Exists(imagePath) ||
                !IOService.IsImageFile(imagePath))
            {
                return null;
            }
            
            try
            {
                return new(imagePath);
            }
            catch (Exception ex)
            {
                //will fail if image is corrupt
                Logger.Error($"Failed to generate a cover for {imagePath}", ex);
                return null;
            }
        }

        public static void CreateThumbnail(Codex c, IMagickImage? image = null)
        {
            if (string.IsNullOrEmpty(c.ThumbnailPath))
            {
                Logger.Error("Trying to write thumbnail to empty path", new InvalidOperationException());
                return;
            }

            uint newWidth = ThumbnailWidth; //sets resolution of thumbnail in pixels
            bool ownsImage = false;

            if (image is null)
            {
                if (!Path.Exists(c.CoverArtPath)) return;
                image = new MagickImage(c.CoverArtPath);
                ownsImage = true;
            }

            try
            {
                //preserve aspect ratio
                uint width = image.Width;
                uint height = image.Height;
                uint newHeight = newWidth / width * height;
                //create thumbnail
                if (IOService.EnsureDirectoryExists(c.ThumbnailPath))
                {
                    image.Thumbnail(newWidth, newHeight);
                    image.Write(c.ThumbnailPath);
                }
            }
            finally
            {
                if (ownsImage)
                {
                    image.Dispose();
                }
            }
        }

        //Take screenshot of specific html element 
        public static IMagickImage GetCroppedScreenShot(IWebDriver driver, IWebElement webElement)
            => GetCroppedScreenShot(driver, webElement.Location, webElement.Size);

        public static IMagickImage GetCroppedScreenShot(IWebDriver driver, System.Drawing.Point location, System.Drawing.Size size)
        {
            //take the screenshot
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            var mf = new MagickImageFactory();
            using var img = mf.Create(ss.AsByteArray);
            img.Resize(3000, 3000); //same size as headless window
            return img.CloneArea(location.X, location.Y, (uint)size.Width, (uint)size.Height);
        }
    }
}
