using Microsoft.Extensions.FileSystemGlobbing;

namespace COMPASS.Infra.Tools;

public static class PathUtils
{
    /// <summary>
    /// How paths are compared on this platform: the filesystem is
    /// case-insensitive on Windows, case-sensitive elsewhere.
    /// </summary>
    public static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    /// <summary>
    /// Canonical form for comparison: absolute, native separators, no trailing
    /// separator (except roots), ".." resolved. Never throws: falls back to a
    /// lightly trimmed input for paths GetFullPath rejects.
    /// For output shape, not comparison, keep the original string.
    /// </summary>
    public static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception)
        {
            fullPath = path.Trim();
        }

        string? root = Path.GetPathRoot(fullPath);
        if (!string.IsNullOrEmpty(root) && fullPath.Length > root.Length)
        {
            fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        return fullPath;
    }

    /// <summary>
    /// Null-safe, normalized, platform-aware path equality. Either null is never equal.
    /// </summary>
    public static bool PathsEqual(string? pathA, string? pathB)
    {
        if (pathA is null || pathB is null) return false;
        return string.Equals(NormalizePath(pathA), NormalizePath(pathB), PathComparison);
    }

    /// <summary>
    /// True when child lies strictly inside parent (separator-boundary aware, so
    /// siblings with a shared prefix like /a/bar1 vs /a/bar do not match).
    /// </summary>
    /// <param name="path">The path that might lie inside <paramref name="directory"/>.</param>
    /// <param name="directory">The directory that might contain <paramref name="path"/>.</param>
    public static bool IsPathInsideDirectory(string? path, string? directory)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(directory)) return false;

        string dirPath = NormalizePath(directory);
        string prefix = dirPath.EndsWith(Path.DirectorySeparatorChar) ? dirPath : dirPath + Path.DirectorySeparatorChar;
        return NormalizePath(path).StartsWith(prefix, PathComparison);
    }

    /// <summary>
    /// Get the longest common path shared by all the given paths
    /// </summary>
    /// <param name="paths"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetCommonFolder(List<string> paths)
    {
        if (paths is null) throw new ArgumentNullException(nameof(paths));
        if (paths.Count == 0) return string.Empty;

        char[] separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
        List<string> referenceSegments = paths.First().Split(separators).ToList();
        //Drop trailing empties from trailing separators (but keep a leading one: it marks an absolute root)
        while (referenceSegments.Count > 1 && referenceSegments[^1].Length == 0)
        {
            referenceSegments.RemoveAt(referenceSegments.Count - 1);
        }
        List<string> commonSegments = [];
        foreach (string segment in referenceSegments)
        {
            List<string> candidateSegments = [.. commonSegments, segment];
            string candidatePath = string.Join(Path.DirectorySeparatorChar, candidateSegments);
            bool sharedByAll = paths.All(path =>
                PathsEqual(path, candidatePath) || IsPathInsideDirectory(path, candidatePath));
            if (sharedByAll)
            {
                commonSegments = candidateSegments;
            }
            else
            {
                break;
            }
        }

        return string.Join(Path.DirectorySeparatorChar, commonSegments);
    }

    /// <summary>
    /// Returns the paths without the longest common end
    /// </summary>
    /// <param name="path1"></param>
    /// <param name="path2"></param>
    /// <returns></returns>
    public static (string, string) GetDifferingRoot(string path1, string path2)
    {
        ArgumentNullException.ThrowIfNull(path1);
        ArgumentNullException.ThrowIfNull(path2);

        if (path1 == path2)
        {
            return (path1, path2);
        }

        if (PathsEqual(path1, path2))
        {
            return (path1, path2);
        }

        char[] separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
        var dirs1 = path1.Split(separators);
        var dirs2 = path2.Split(separators);

        int shortestLength = Math.Min(dirs1.Length, dirs2.Length);

        int commonDirs = 0;
        while (commonDirs < shortestLength //while we have not iterated over the entire shortest array
                && dirs1.TakeLast(commonDirs + 1)
                    .SequenceEqual(dirs2.TakeLast(commonDirs + 1), PathComparer)) //and the last x elements are the same
        {
            commonDirs++;
        }

        string remainingPath1 = string.Join(Path.DirectorySeparatorChar, dirs1.Take(dirs1.Length - commonDirs));
        string remainingPath2 = string.Join(Path.DirectorySeparatorChar, dirs2.Take(dirs2.Length - commonDirs));

        return (remainingPath1, remainingPath2);
    }

    public static IEnumerable<string> GetAllParentDirectories(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) yield break;

        //Native separators only: on Linux a backslash is a valid filename character,
        //not a separator, so only Windows folds the alt separator
        if (OperatingSystem.IsWindows())
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
        string trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.IsNullOrEmpty(trimmedPath)) yield break;

        var parts = trimmedPath.Split(Path.DirectorySeparatorChar);
        for (int i = 1; i < parts.Length; i++)
        {
            string parent = string.Join(Path.DirectorySeparatorChar, parts.SkipLast(i));
            if (!string.IsNullOrEmpty(parent))
            {
                yield return parent;
            }
        }
    }

    public static bool MatchesAnyGlob(string filePath, IList<string> globs )
    {
        if (string.IsNullOrEmpty(filePath)) return false;
            
        // Check for exact matches first
        if (globs.Any(glob => glob == filePath))
        {
            return true;
        }

        var matcher = new Matcher();
        matcher.AddIncludePatterns(globs);
        return matcher.Match(filePath).HasMatches;
    }
}