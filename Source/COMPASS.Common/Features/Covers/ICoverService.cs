using COMPASS.Common.Features.Metadata;
using ImageMagick;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Covers;

public interface ICoverService
{
    /// <summary>
    /// Fetches a cover image for the given codex
    /// </summary>
    /// <exception cref="System.OperationCanceledException"></exception>
    Task FetchAndApplyCover(Codex codex, ChooseMetadataViewModel? chooseMetadataViewModel = null, CancellationToken cancellationToken = default);

    Task FetchAndApplyCovers(List<Codex> codices);

    Task ApplyCover(Codex destCodex, string imagePath, CancellationToken cancellationToken = default);
    Task ApplyCover(Codex destCodex, IMagickImage image, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures an up to date thumbnail exists on disk for the given codex.
    /// Regens if outdated, missing or corrupt. 
    /// </summary>
    string? EnsureThumbnailFile(Codex codex);
}
