using COMPASS.Infra.IO;
using System.IO.Compression;

namespace COMPASS.UnitTests.Infra.IO;

[TestFixture]
public class PathUtilsTests
{
    #region GetCommonFolder

    [Test]
    public void GetCommonFolder_SinglePath_ReturnsFullPath()
    {
        string path = Path.Combine("Users", "docs", "file.txt");
        List<string> paths = [path];

        string result = PathUtils.GetCommonFolder(paths);

        Assert.That(result, Is.EqualTo(path));
    }

    [Test]
    public void GetCommonFolder_CommonPrefix()
    {
        List<string> paths =
        [
            Path.Combine("Users", "docs", "a.txt"),
            Path.Combine("Users", "docs", "b.txt"),
            Path.Combine("Users", "docs", "sub", "c.txt")
        ];

        string result = PathUtils.GetCommonFolder(paths);

        Assert.That(result, Is.EqualTo(Path.Combine("Users", "docs")));
    }

    [Test]
    public void GetCommonFolder_NoCommon_ReturnsEmpty()
    {
        List<string> paths =
        [
            Path.Combine("rootA", "file.txt"),
            Path.Combine("rootB", "file.txt")
        ];

        string result = PathUtils.GetCommonFolder(paths);

        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void GetCommonFolder_EmptyList_ReturnsEmpty()
    {
        Assert.That(PathUtils.GetCommonFolder([]), Is.EqualTo(""));
    }

    [Test]
    public void GetCommonFolder_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PathUtils.GetCommonFolder(null!));
    }

    [Test]
    public void GetCommonFolder_AbsolutePath_ReturnsFullPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Drive-absolute paths are a Windows concept");
        }

        string path = Path.Combine(@"C:\", "Users", "pauld", "books", "a.epub");

        Assert.That(PathUtils.GetCommonFolder([path]), Is.EqualTo(path));
    }

    [Test]
    public void GetCommonFolder_AbsolutePaths_ReturnsCommonDirectory()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Drive-absolute paths are a Windows concept");
        }

        //Regression: the leading "C:" segment used to be normalized on its own,
        //where Path.GetFullPath("C:") resolves to the current directory instead of
        //the drive root, so no common folder was ever found.
        List<string> paths =
        [
            Path.Combine(@"C:\", "Users", "pauld", "books", "a.epub"),
            Path.Combine(@"C:\", "Users", "pauld", "books", "sub", "b.epub"),
            Path.Combine(@"C:\", "Users", "pauld", "books", "c.epub")
        ];

        Assert.That(PathUtils.GetCommonFolder(paths),
            Is.EqualTo(Path.Combine(@"C:\", "Users", "pauld", "books")));
    }

    [Test]
    public void GetCommonFolder_DifferentDrives_ReturnsEmpty()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Drive-absolute paths are a Windows concept");
        }

        List<string> paths =
        [
            Path.Combine(@"C:\", "books", "a.epub"),
            Path.Combine(@"D:\", "books", "b.epub")
        ];

        Assert.That(PathUtils.GetCommonFolder(paths), Is.EqualTo(""));
    }

    [Test]
    public void GetCommonFolder_RootAbsolutePaths_ReturnsCommonDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Root-absolute POSIX paths only");
        }

        List<string> paths =
        [
            Path.Combine("/", "home", "user", "books", "a.epub"),
            Path.Combine("/", "home", "user", "books", "sub", "b.epub")
        ];

        Assert.That(PathUtils.GetCommonFolder(paths),
            Is.EqualTo(Path.Combine("/", "home", "user", "books")));
    }

    #endregion

    #region GetDifferingRoot

    [Test]
    public void GetDifferingRoot_Normal()
    {
        string path1 = Path.Combine("a", "path", "to", "a", "file.txt");
        string path2 = Path.Combine("another", "root", "that goes", "to", "a", "file.txt");

        (string result1, string result2) = PathUtils.GetDifferingRoot(path1, path2);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.EqualTo(Path.Combine("a", "path")));
            Assert.That(result2, Is.EqualTo(Path.Combine("another", "root", "that goes")));
        });
    }

    [Test]
    public void GetDifferingRoot_Same()
    {
        string path1 = Path.Combine("a", "path", "to", "a", "file.txt");

        (string result1, string result2) = PathUtils.GetDifferingRoot(path1, path1);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.EqualTo(result2));
            Assert.That(result1, Is.EqualTo(path1));
        });
    }

    [Test]
    public void GetDifferingRoot_Subpath()
    {
        string path1 = Path.Combine("a", "path", "to", "a", "file.txt");
        string path2 = Path.Combine("a", "file.txt");

        (string result1, string result2) = PathUtils.GetDifferingRoot(path1, path2);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.EqualTo(Path.Combine("a", "path", "to")));
            Assert.That(result2, Is.EqualTo(""));
        });
    }

    [Test]
    public void GetDifferingRoot_NullPath1_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PathUtils.GetDifferingRoot(null!, Path.Combine("a", "path", "file.txt")));
    }

    [Test]
    public void GetDifferingRoot_NullPath2_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PathUtils.GetDifferingRoot(Path.Combine("a", "path", "file.txt"), null!));
    }

    [Test]
    public void GetDifferingRoot_NoCommonEnd_ReturnsBothPathsUnchanged()
    {
        string path1 = Path.Combine("root1", "file.txt");
        string path2 = Path.Combine("root2", "other.txt");

        (string result1, string result2) = PathUtils.GetDifferingRoot(path1, path2);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.EqualTo(path1));
            Assert.That(result2, Is.EqualTo(path2));
        });
    }

    #endregion

    #region GetAllParentDirectories

    [Test]
    public void GetAllParentDirectories_Normal()
    {
        string path = Path.Combine("a", "path", "to", "a", "file.txt");
        List<string> result = PathUtils.GetAllParentDirectories(path).ToList();
        Assert.That(result, Is.EqualTo(new List<string>
        {
            Path.Combine("a", "path", "to", "a"),
            Path.Combine("a", "path", "to"),
            Path.Combine("a", "path"),
            "a"
        }));
    }

    [Test]
    public void GetAllParentDirectories_Empty()
    {
        string path = "";
        List<string> result = PathUtils.GetAllParentDirectories(path).ToList();
        Assert.That(result, Is.EqualTo(new List<string>()));
    }

    #endregion

    #region PathsEqual

    [Test]
    public void PathsEqual_SamePath_ReturnsTrue()
    {
        string path = Path.Combine("a", "books", "file.txt");

        Assert.That(PathUtils.PathsEqual(path, path), Is.True);
    }

    [Test]
    public void PathsEqual_TrailingSlash_Ignored()
    {
        string path = Path.Combine("a", "books");

        Assert.That(PathUtils.PathsEqual(path, path + Path.DirectorySeparatorChar), Is.True);
    }

    [Test]
    public void PathsEqual_DifferentCasing_MatchesOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Path casing is only insignificant on Windows");
        }

        Assert.That(PathUtils.PathsEqual(
            Path.Combine("A", "Books"),
            Path.Combine("a", "books")), Is.True);
    }

    [Test]
    public void PathsEqual_MixedSeparators_MatchesOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Backslash is a separator only on Windows");
        }

        Assert.That(PathUtils.PathsEqual(@"a\books\file.txt", "a/books/file.txt"), Is.True);
    }

    [TestCase(null, null)]
    [TestCase(null, "a")]
    [TestCase("a", null)]
    public void PathsEqual_Null_ReturnsFalse(string? pathA, string? pathB)
    {
        Assert.That(PathUtils.PathsEqual(pathA, pathB), Is.False);
    }

    #endregion

    #region IsPathInsideDirectory

    [Test]
    public void IsPathInsideDirectory_DirectChild_ReturnsTrue()
    {
        Assert.That(PathUtils.IsPathInsideDirectory(
            Path.Combine("a", "books", "scifi"),
            Path.Combine("a", "books")), Is.True);
    }

    [Test]
    public void IsPathInsideDirectory_SiblingWithSharedPrefix_ReturnsFalse()
    {
        // /a/bar1 is not inside /a/bar despite the string prefix
        Assert.That(PathUtils.IsPathInsideDirectory(
            Path.Combine("a", "bar1"),
            Path.Combine("a", "bar")), Is.False);
    }

    [Test]
    public void IsPathInsideDirectory_SamePath_ReturnsFalse()
    {
        string path = Path.Combine("a", "books");

        Assert.That(PathUtils.IsPathInsideDirectory(path, path), Is.False);
    }

    [TestCase(null, "a")]
    [TestCase("a", null)]
    [TestCase("a", "")]
    [TestCase("", "a")]
    public void IsPathInsideDirectory_NullOrEmpty_ReturnsFalse(string? child, string? parent)
    {
        Assert.That(PathUtils.IsPathInsideDirectory(child, parent), Is.False);
    }

    #endregion

    #region NormalizePath

    [Test]
    public void NormalizePath_GoodPath_Untouched()
    {
        string path = GetRootedPath("books", "file.txt");
        string normalized = PathUtils.NormalizePath(path);

        Assert.That(normalized, Is.EqualTo(path));
    }

    [Test]
    public void NormalizePath_ForwardSlashes_NormalizedOnWindows()
    {
        //Only Windows has a second separator ('/') that NormalizePath repairs:
        //on Unix both DirectorySeparatorChar and AltDirectorySeparatorChar are '/',
        //and a backslash is an ordinary filename character rather than a separator.
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Forward slash is the only separator on Unix, so there is no wrong separator to repair");
        }

        string path = GetRootedPath("books", "file.txt");
        string brokenPath = path.Replace('\\', '/');

        Assert.That(brokenPath, Is.Not.EqualTo(path));

        string normalized = PathUtils.NormalizePath(brokenPath);

        Assert.That(normalized, Is.EqualTo(path));
    }

    #endregion

    #region Portable relative paths

    [Test]
    public void ToPortablePath_NativeSeparators_ConvertedToForwardSlashes()
    {
        string path = Path.Combine("books", "sub", "file.txt");

        Assert.That(PathUtils.ToPortablePath(path), Is.EqualTo("books/sub/file.txt"));
    }

    [Test]
    public void FromPortablePath_ForwardSlashes_ConvertedToNativeSeparators()
    {
        string path = PathUtils.FromPortablePath("books/sub/file.txt");

        Assert.That(path, Is.EqualTo(Path.Combine("books", "sub", "file.txt")));
    }

    [Test]
    public void PortablePath_RoundTripsToOriginal()
    {
        string path = Path.Combine("books", "sub", "file.txt");

        string portable = PathUtils.ToPortablePath(path);

        Assert.That(portable, Does.Not.Contain('\\'));
        Assert.That(PathUtils.FromPortablePath(portable), Is.EqualTo(path));
    }

    [Test]
    public void ToPortablePath_BackslashOnUnix_Untouched()
    {
        //On Linux a backslash is a legal character in a file name, not a separator,
        //so it must not be rewritten.
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Backslash is a directory separator on Windows");
        }

        Assert.That(PathUtils.ToPortablePath("books\\sub"), Is.EqualTo("books\\sub"));
    }

    [Test]
    public void ToPortablePath_ForwardSlashOnWindows_StaysForwardSlash()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Forward slash is the native separator on Unix");
        }

        Assert.That(PathUtils.ToPortablePath("books/sub"), Is.EqualTo("books/sub"));
    }

    #endregion

    #region GetCommonFolder prefix fix

    [Test]
    public void GetCommonFolder_SiblingWithSharedPrefix_StopsAtParent()
    {
        List<string> paths =
        [
            Path.Combine("a", "bar", "x.txt"),
            Path.Combine("a", "bar1", "y.txt")
        ];

        Assert.That(PathUtils.GetCommonFolder(paths), Is.EqualTo("a"));
    }

    [Test]
    public void GetCommonFolder_TrailingSlash_Tolerated()
    {
        List<string> paths =
        [
            Path.Combine("a", "books") + Path.DirectorySeparatorChar,
            Path.Combine("a", "books", "scifi")
        ];

        Assert.That(PathUtils.GetCommonFolder(paths), Is.EqualTo(Path.Combine("a", "books")));
    }

    #endregion

    #region GetDifferingRoot case fix

    [Test]
    public void GetDifferingRoot_CaseDifference_MatchesOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Path casing is only insignificant on Windows");
        }

        string path1 = Path.Combine("A", "path", "to", "file.txt");
        string path2 = Path.Combine("a", "other", "to", "file.txt");

        (string result1, string result2) = PathUtils.GetDifferingRoot(path1, path2);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.EqualTo(Path.Combine("A", "path")));
            Assert.That(result2, Is.EqualTo(Path.Combine("a", "other")));
        });
    }

    #endregion

    #region GetAllParentDirectories normalization

    [Test]
    public void GetAllParentDirectories_TrailingSlash_NoEmptyEntries()
    {
        string path = Path.Combine("a", "books") + Path.DirectorySeparatorChar;

        Assert.That(PathUtils.GetAllParentDirectories(path).ToList(),
            Is.EqualTo(new List<string> { Path.Combine("a") }));
    }

    #endregion

    #region MatchesAnyGlob

    [Test]
    public void MatchesAnyGlob_ExactMatch_ReturnsTrue()
    {
        Assert.That(PathUtils.MatchesAnyGlob("file.txt", ["file.txt"]), Is.True);
    }

    [Test]
    public void MatchesAnyGlob_GlobPattern_Matches()
    {
        Assert.That(PathUtils.MatchesAnyGlob(Path.Combine("docs", "readme.md"), ["**/*.md"]), Is.True);
    }

    [Test]
    public void MatchesAnyGlob_AbsolutePath_MatchesFolderGlob()
    {
        string filePath = GetRootedPath("Users", "pauld", "Documents", "TTRPG", "A subfolder", "anotherFolder", "test.txt");

        Assert.That(PathUtils.MatchesAnyGlob(filePath, ["**/A subfolder/**"]), Is.True);
    }

    [Test]
    public void MatchesAnyGlob_AbsolutePath_MatchesExtensionGlob()
    {
        string filePath = GetRootedPath("Users", "pauld", "Documents", "A subfolder", "test.txt");

        Assert.That(PathUtils.MatchesAnyGlob(filePath, ["**/*.txt"]), Is.True);
    }

    [Test]
    public void MatchesAnyGlob_AbsolutePath_NoMatch_ReturnsFalse()
    {
        string filePath = GetRootedPath("Users", "pauld", "Documents", "A subfolder", "test.txt");

        Assert.That(PathUtils.MatchesAnyGlob(filePath, ["**/Other Tag/**"]), Is.False);
    }

    [Test]
    public void MatchesAnyGlob_RelativePathWithParentTraversal_MatchesFolderGlob()
    {
        string filePath = Path.Combine("..", "TTRPG", "A subfolder", "anotherFolder", "test.txt");

        Assert.That(PathUtils.MatchesAnyGlob(filePath, ["**/A subfolder/**"]), Is.True);
    }

    [Test]
    public void MatchesAnyGlob_GlobPattern_Matches_FileWithoutDirectory()
    {
        Assert.That(PathUtils.MatchesAnyGlob("readme.md", ["*.md"]), Is.True);
    }

    [Test]
    public void MatchesAnyGlob_GlobPattern_NoMatch_FileWithoutDirectory()
    {
        Assert.That(PathUtils.MatchesAnyGlob("readme.md", ["*.cs"]), Is.False);
    }

    [Test]
    public void MatchesAnyGlob_NoMatch_ReturnsFalse()
    {
        Assert.That(PathUtils.MatchesAnyGlob(Path.Combine("docs", "file.txt"), ["*.cs"]), Is.False);
    }

    [Test]
    public void MatchesAnyGlob_EmptyGlobList_ReturnsFalse()
    {
        Assert.That(PathUtils.MatchesAnyGlob(Path.Combine("docs", "file.txt"), []), Is.False);
    }

    [Test]
    public void MatchesAnyGlob_MultipleGlobs_MatchesSecond()
    {
        Assert.That(PathUtils.MatchesAnyGlob(Path.Combine("docs", "file.txt"), ["*.cs", "**/*.txt"]), Is.True);
    }

    [Test]
    public void MatchesAnyGlob_MultipleGlobs_NoneMatch_ReturnsFalse()
    {
        Assert.That(PathUtils.MatchesAnyGlob(Path.Combine("docs", "file.txt"), ["*.cs", "*.md"]), Is.False);
    }

    [Test]
    public void MatchesAnyGlob_MultipleGlobs_ExactMatchAmongPatterns()
    {
        Assert.That(PathUtils.MatchesAnyGlob(Path.Combine("docs", "file.txt"), ["*.cs", Path.Combine("docs", "file.txt")]), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    public void MatchesAnyGlob_NullOrEmptyPath_ReturnsFalse(string? path)
    {
        Assert.That(PathUtils.MatchesAnyGlob(path!, ["*"]), Is.False);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Builds an absolute path using this platform's root: a drive root such as
    /// "C:\" on Windows, "/" elsewhere. A Windows drive path is a relative path
    /// on Unix, so Path.GetFullPath would resolve it against the working directory.
    /// </summary>
    private static string GetRootedPath(params string[] segments)
    {
        string volumeRoot = OperatingSystem.IsWindows() ? @"C:\" : "/";

        return Path.Combine([volumeRoot, .. segments]);
    }

    #endregion
}
