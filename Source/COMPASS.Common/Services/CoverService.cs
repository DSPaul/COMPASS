using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Sources;
using COMPASS.Common.ViewModels;
using ImageMagick;
using ImageMagick.Factories;
using OpenQA.Selenium;
using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Infra.Tools;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Operations;

namespace COMPASS.Common.Services
{
    public static class CoverService
    {
        private static ILogger? _logger;
        private static ILogger Logger => _logger ??= ServiceResolver.Resolve<ILogger>();

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
            IMagickImage<byte>? coverFromSource = null;
            try
            {
                CodexProperty coverProp = ServiceResolver.Resolve<IPreferencesService>().Preferences.ImportableCodexProperties.First(prop => prop.Name == nameof(SourceMetaData.Cover));

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

                    MetaDataSource? source = MetaDataSource.GetSource(sourceType, codex.Collection);
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
                    newMetaData.Cover = new MagickImage(coverFromSource);
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
            if (!codices.Any()) return;

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
                await WindowManager.OpenModal(chooseMetaDataVM);
            }
        }

        public static async Task SaveCover(Codex destCodex, IMagickImage image)
        {
            if (string.IsNullOrEmpty(destCodex.CoverArtPath))
            {
                Logger.Error("Trying to write cover img to empty path", new InvalidOperationException());
                return;
            }

            var ioService = ServiceResolver.Resolve<IIOService>();
            
            if (image.Width > CoverWidth) image.Resize(CoverWidth, 0);
            
            if (ioService.EnsureDirectoryExists(destCodex.CoverArtPath))
            {
                await image.WriteAsync(destCodex.CoverArtPath);
                CreateThumbnail(destCodex, image);
                destCodex.NotifyCoverChanged();
            }
        }

        public static MagickImage? GetCoverFromImage(string imagePath)
        {
            //check if it's a valid file
            if (string.IsNullOrEmpty(imagePath) ||
                !Path.Exists(imagePath) ||
                !FileFormatUtils.IsImageFile(imagePath))
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

        public static IMagickImage? CreateThumbnail(Codex c, IMagickImage? image = null)
        {
            uint newWidth = ThumbnailWidth; //sets resolution of thumbnail in pixels

            if (image is null)
            {
                if (!File.Exists(c.CoverArtPath))
                {
                    return null;
                }

                try
                {
                    image = new MagickImage(c.CoverArtPath);
                }
                catch(MagickCorruptImageErrorException corruptException)
                {
                    HandleCorruptCover(c, corruptException);
                    return null;
                }
            }
            
            var ioService = ServiceResolver.Resolve<IIOService>();

            //preserve aspect ratio
            uint width = image.Width;
            uint height = image.Height;
            uint newHeight = newWidth / width * height;
            image.Thumbnail(newWidth, newHeight);

            //create thumbnail
            if (ioService.EnsureDirectoryExists(c.ThumbnailPath))
            {
                image.Write(c.ThumbnailPath);
            }

            return image;
        }

        private static void HandleCorruptCover(Codex codex, MagickCorruptImageErrorException corruptException)
        {
            Logger.Warn("Corrupt cover detected", corruptException);
            if (!codex.CoverArtPath.Contains(Constants.DIR_COVERS))
            {
                //not our image, don't delete it
                return;
            }

            try
            {
                File.Delete(codex.CoverArtPath);
                Logger.Info("Corrupt cover removed, attempting to fetch a new cover...");
                CodexOperations.GetCover(codex).ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        CreateThumbnail(codex);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete corrupt image file {codex.CoverArtPath}", ex);
            }
        }

        //Take screenshot of specific html element 
        public static IMagickImage<byte> GetCroppedScreenShot(IWebDriver driver, IWebElement webElement)
            => GetCroppedScreenShot(driver, webElement.Location, webElement.Size);

        public static IMagickImage<byte> GetCroppedScreenShot(IWebDriver driver, System.Drawing.Point location, System.Drawing.Size size)
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
