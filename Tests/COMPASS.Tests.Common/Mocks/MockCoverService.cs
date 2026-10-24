using COMPASS.Common.Domain;
using COMPASS.Common.Features.Covers;
using COMPASS.Common.Features.Metadata;
using ImageMagick;

namespace COMPASS.Tests.Common.Mocks;

/// <summary>
/// Does nothing, so tests that import or export a collection don't go looking for covers at online metadata sources
/// </summary>
public class MockCoverService : ICoverService
{
    public Task FetchAndApplyCover(Codex codex, ChooseMetadataViewModel? chooseMetadataViewModel = null,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task FetchAndApplyCovers(IList<Codex> codices) => Task.CompletedTask;

    public Task ApplyCover(Codex destCodex, string imagePath, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ApplyCover(Codex destCodex, IMagickImage image, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public string? EnsureThumbnailFile(Codex codex) => null;
}
