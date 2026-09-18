using Autofac.Features.Indexed;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Sources;
using COMPASS.Infra.Models.Measuring;
using COMPASS.Infra.Models.Progress;
using ImageMagick;
using ImageMagick.Factories;
using OpenQA.Selenium;
using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Infra.Tools;
using COMPASS.Infra.Tools.Logging;

namespace COMPASS.Common.Services
{
    public class CoverService(
        ILogger logger,
        IPreferencesService preferencesService,
        IIOService ioService,
        ChooseMetaDataViewModelFactory chooseMetaDataViewModelFactory,
        ProgressTrackingManager progressTrackingManager,
        IIndex<string, MetaDataSource> metaDataSources) : ICoverService
    {

        private const int ThumbnailWidth = 200;
        private const int CoverWidth = 850;

        /// <summary>
        /// Fetches a cover image for the given codex
        /// </summary>
        /// <param name="codex"></param>
        /// <param name="chooseMetaDataViewModel"></param>
        /// <exception cref="System.OperationCanceledException"></exception>
        public async Task GetAndApplyCover(Codex codex, ChooseMetaDataViewModel? chooseMetaDataViewModel = null, CancellationToken ct = default)
        {
            //TODO add visual feedback while fetching, like a spinner on the thumbnail
            IMagickImage<byte>? coverFromSource = null;
            try
            {
                CodexProperty coverProp = preferencesService.Preferences.ImportableCodexProperties.First(prop => prop.Name == nameof(SourceMetaData.Cover));

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
                    ct.ThrowIfCancellationRequested();

                    if (!metaDataSources.TryGetValue(sourceType.ToString(), out MetaDataSource? source)) continue;
                    if (!source.IsValidSource(codex.Sources)) continue;
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
            }
        }

        public async Task GetAndApplyCover(List<Codex> codices)
        {
            if (!codices.Any()) return;

            ProgressTracker progressTracker = new(Quantities.Items())
            {
                StatusMessage = "Getting covers...",
                Total = codices.Count
            };

            ChooseMetaDataViewModel chooseMetaDataVM = chooseMetaDataViewModelFactory.Create();

            try
            {
                await progressTrackingManager.RunAsync(progressTracker, "Getting covers",
                    async (tracker, ct) =>
                    {
                        ParallelOptions workerOptions = new()
                        {
                            MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount / 2, 1),
                            CancellationToken = ct
                        };
                        await Parallel.ForEachAsync(codices, workerOptions, async (codex, ct) =>
                        {
                            try
                            {
                                await GetAndApplyCover(codex, chooseMetaDataVM, ct);
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                logger.Error($"Failed to fetch cover for {codex.Title}", ex);
                            }
                            finally
                            {
                                tracker.Report(ProgressReports.Increment);
                            }
                        });
                    });
            }
            catch (OperationCanceledException)
            {
                logger.Info("Cover fetching was canceled");
                return;
            }
            catch (Exception ex)
            {
                logger.Error("Failed to fetch covers", ex);
            }

            if (chooseMetaDataVM.MetaDataProposals.Any())
            {
                await WindowManager.OpenModal(chooseMetaDataVM);
            }
        }

        public async Task SaveCover(Codex destCodex, IMagickImage image)
        {
            if (string.IsNullOrEmpty(destCodex.CoverArtPath))
            {
                logger.Error("Trying to write cover img to empty path", new InvalidOperationException());
                return;
            }
            
            if (image.Width > CoverWidth) image.Resize(CoverWidth, 0);
            
            if (ioService.EnsureDirectoryExists(destCodex.CoverArtPath))
            {
                await image.WriteAsync(destCodex.CoverArtPath);
                CreateThumbnail(destCodex, image);
                destCodex.NotifyCoverChanged();
            }
        }

        public MagickImage? GetCoverFromImage(string imagePath)
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
                logger.Error($"Failed to generate a cover for {imagePath}", ex);
                return null;
            }
        }

        public IMagickImage? CreateThumbnail(Codex c, IMagickImage? image = null)
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

        private void HandleCorruptCover(Codex codex, MagickCorruptImageErrorException corruptException)
        {
            logger.Warn("Corrupt cover detected", corruptException);
            if (!codex.CoverArtPath.Contains(Constants.DIR_COVERS))
            {
                //not our image, don't delete it
                return;
            }

            try
            {
                File.Delete(codex.CoverArtPath);
                logger.Info("Corrupt cover removed, attempting to fetch a new cover...");
                _ = ReapplyCoverAfterCorruptionAsync(codex);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to delete corrupt image file {codex.CoverArtPath}", ex);
            }
        }

        private async Task ReapplyCoverAfterCorruptionAsync(Codex codex)
        {
            try
            {
                await GetAndApplyCover([codex]);
                // Explicitly create a new thumbnail as an old corrupt one might still exist
                CreateThumbnail(codex);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to fetch a new cover for {codex.Title} after removing the corrupt one", ex);
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
