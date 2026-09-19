using COMPASS.Common.Models;
using COMPASS.Common.ViewModels.Modals;
using ImageMagick;

namespace COMPASS.Common.Interfaces.Services;

public interface ICoverService
{
    /// <summary>
    /// Fetches a cover image for the given codex
    /// </summary>
    /// <exception cref="System.OperationCanceledException"></exception>
    Task GetAndApplyCover(Codex codex, ChooseMetadataViewModel? chooseMetadataViewModel = null, CancellationToken cancellationToken = default);

    Task GetAndApplyCover(List<Codex> codices);

    Task SaveCover(Codex destCodex, IMagickImage image);

    MagickImage? GetCoverFromImage(string imagePath);

    IMagickImage? CreateThumbnail(Codex c, IMagickImage? image = null);
}
