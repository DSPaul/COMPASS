namespace COMPASS.Infra.IO;

/// <summary>
/// Paths handed over by an external source (a drop, a clipboard, a stored reference), sorted by
/// what they are on disk. Such a path can point at something that no longer exists, and a path
/// that does not exist has no attributes to read: <see cref="File.GetAttributes"/> throws
/// <see cref="FileNotFoundException"/> instead of answering, so what a path is has to be
/// established by checking existence first.
/// </summary>
public sealed class PathClassification
{
    /// <summary>
    /// Paths that are folders.
    /// </summary>
    public List<string> Folders { get; } = [];

    /// <summary>
    /// Paths that are files.
    /// </summary>
    public List<string> Files { get; } = [];

    /// <summary>
    /// Paths that do not exist (anymore): deleted, moved, or never on disk to begin with.
    /// </summary>
    public List<string> Missing { get; } = [];
}
