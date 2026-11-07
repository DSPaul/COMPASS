using COMPASS.Common.Features.Codices;

namespace COMPASS.Common.Features.Layouts;

/// <summary>
/// Carries the codices a layout wants selected, so its view can apply that selection to its list or grid.
/// </summary>
public class CodexSelectionEventArgs(IReadOnlyCollection<CodexViewModel> codices) : EventArgs
{
    public IReadOnlyCollection<CodexViewModel> Codices { get; } = codices;
}
