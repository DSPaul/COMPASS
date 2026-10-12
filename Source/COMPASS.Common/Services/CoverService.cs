using Autofac.Features.Indexed;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Sources;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Infra.Avalonia.Modal;
using COMPASS.Infra.Image;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Measuring;
using COMPASS.Infra.Preferences;
using COMPASS.Infra.Progress;
using ImageMagick;
using System.Diagnostics;

namespace COMPASS.Common.Services
{
    public class CoverService(
        ILogger logger,
        IPreferencesService preferencesService,
        IImageService imageService,
        IIOService ioService,
        ICoverStorageService coverStorageService,
        ChooseMetadataViewModelFactory chooseMetadataViewModelFactory,
        ProgressTrackingManager progressTrackingManager,
        IIndex<string, MetadataSource> metaDataSources) : ICoverService
    {

        private const int ThumbnailWidth = 200;
        private const int CoverWidth = 850;

        /// <summary>
        /// Fetches a cover image for the given codex
        /// </summary>
        /// <param name="codex"></param>
        /// <param name="chooseMetadataViewModel"></param>
        /// <exception cref="System.OperationCanceledException"></exception>
        public async Task FetchAndApplyCover(Codex codex, ChooseMetadataViewModel? chooseMetadataViewModel = null, CancellationToken ct = default)
        {
            //TODO add visual feedback while fetching, like a spinner on the thumbnail
            Preferences preferences = preferencesService.GetPreferences<Preferences>();
            IMagickImage<byte>? coverFromSource = null;
            try
            {
                CodexProperty coverProp = preferences.ImportableCodexProperties.First(prop => prop.Name == nameof(SourceMetadata.Cover));

                switch (coverProp.OverwriteMode)
                {
                    case MetadataOverwriteMode.Ask:
                        Debug.Assert(chooseMetadataViewModel is not null, "choose Metadata ViewModel cannot be null if overwrite mode is ask");
                        break;
                    case MetadataOverwriteMode.Never:
                    case MetadataOverwriteMode.IfEmpty when !coverProp.IsEmpty(codex):
                        return;
                }

                bool shouldAsk = coverProp.OverwriteMode == MetadataOverwriteMode.Ask && !coverProp.IsEmpty(codex);


                foreach (var sourceType in coverProp.SourcePriority)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!metaDataSources.TryGetValue(sourceType.ToString(), out MetadataSource? source)) continue;
                    if (!source.IsValidSource(codex.Sources)) continue;
                    coverFromSource = await source.FetchCover(codex.Sources);
                    if (coverFromSource != null) break;
                }

                //no cover was found
                if (coverFromSource == null) return;

                if (shouldAsk)
                {
                    SourceMetadata newMetadata = new()
                    {
                        Cover = coverFromSource,
                    };

                    //check if the image is different from the existing one
                    if (!coverProp.HasNewValue(newMetadata, codex)) return;

                    //make a copy of cover because originals lifetime is limited to this method
                    newMetadata.Cover = new MagickImage(coverFromSource);
                    chooseMetadataViewModel!.AddMetadataProposal(codex, newMetadata);
                }
                else
                {
                    await ApplyCover(codex, coverFromSource, ct);
                }
            }
            finally
            {
                coverFromSource?.Dispose();
            }
        }

        public async Task FetchAndApplyCovers(List<Codex> codices)
        {
            if (!codices.Any()) return;

            ProgressTracker progressTracker = new(Quantities.Items())
            {
                Total = codices.Count
            };

            ChooseMetadataViewModel chooseMetadataVM = chooseMetadataViewModelFactory.Create();

            try
            {
                await progressTrackingManager.RunAsync(progressTracker, "Getting covers...",
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
                                await FetchAndApplyCover(codex, chooseMetadataVM, ct);
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

            if (chooseMetadataVM.MetadataProposals.Any())
            {
                await WindowManager.OpenModal(chooseMetadataVM);
            }
        }

        public async Task ApplyCover(Codex destCodex, string imagePath, CancellationToken cancellationToken = default)
        {
            using var image = await imageService.LoadImageAsync(imagePath, cancellationToken);
            if (image != null)
            {
                await ApplyCover(destCodex, image, cancellationToken);
            }
        }
        public async Task ApplyCover(Codex destCodex, IMagickImage image, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(destCodex.CoverArtPath))
            {
                logger.Error("Trying to write cover img to empty path", new InvalidOperationException());
                return;
            }
            
            if (image.Width > CoverWidth) image.Resize(CoverWidth, 0);
            
            if (ioService.EnsureDirectoryExists(destCodex.CoverArtPath))
            {
                await image.WriteAsync(destCodex.CoverArtPath, cancellationToken);
                CreateThumbnail(destCodex, image)?.Dispose();
                destCodex.NotifyCoverChanged();
            }
        }

        /// <inheritdoc />
        public string? EnsureThumbnailFile(Codex codex)
        {
            ArgumentNullException.ThrowIfNull(codex);

            string thumbnailPath = coverStorageService.GetThumbnailPath(codex);

            // Invalidate stale thumbnail: if the cover art was updated after the thumbnail was generated, delete it
            if (File.Exists(thumbnailPath) && File.Exists(codex.CoverArtPath) &&
                File.GetLastWriteTimeUtc(codex.CoverArtPath) > File.GetLastWriteTimeUtc(thumbnailPath))
            {
                File.Delete(thumbnailPath);
            }

            if (File.Exists(thumbnailPath))
            {
                return thumbnailPath;
            }

            if (!File.Exists(codex.CoverArtPath))
            {
                return null;
            }

            IMagickImage? createdThumbnail = CreateThumbnail(codex);
            createdThumbnail?.Dispose();

            return File.Exists(thumbnailPath) ? thumbnailPath : null;
        }

        private IMagickImage? CreateThumbnail(Codex codex, IMagickImage? image = null)
        {
            uint newWidth = ThumbnailWidth; //sets resolution of thumbnail in pixels

            if (image is null)
            {
                if (!File.Exists(codex.CoverArtPath))
                {
                    return null;
                }

                try
                {
                    image = new MagickImage(codex.CoverArtPath);
                }
                catch(MagickCorruptImageErrorException corruptException)
                {
                    HandleCorruptCover(codex, corruptException);
                    return null;
                }
                catch(Exception ex)
                {
                    logger.Error($"Failed to create thumbnail for {codex.Title}", ex);
                    return null;
                }
            }
            
            //preserve aspect ratio
            uint width = image.Width;
            uint height = image.Height;
            uint newHeight = newWidth / width * height;
            image.Thumbnail(newWidth, newHeight);

            //create thumbnail
            string path = coverStorageService.GetThumbnailPath(codex);
            if (ioService.EnsureDirectoryExists(path))
            {
                image.Write(path);
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
                await FetchAndApplyCovers([codex]);
                // Explicitly create a new thumbnail as an old corrupt one might still exist
                CreateThumbnail(codex)?.Dispose();
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to fetch a new cover for {codex.Title} after removing the corrupt one", ex);
            }
        }
    }
}
