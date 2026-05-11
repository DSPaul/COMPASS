using COMPASS.Infra.Tools;

namespace COMPASS.UnitTests.Infra.Tools;

[TestFixture]
public class PathUtilsTests
{
    #region GetCommonFolder

    [Test]
    public void GetCommonFolder_SinglePath_ReturnsFullPath()
    {
        List<string> paths = [@"C:\Users\docs\file.txt"];

        string result = PathUtils.GetCommonFolder(paths);

        Assert.That(result, Is.EqualTo(@"C:\Users\docs\file.txt"));
    }

    [Test]
    public void GetCommonFolder_CommonPrefix()
    {
        List<string> paths =
        [
            @"C:\Users\docs\a.txt",
            @"C:\Users\docs\b.txt",
            @"C:\Users\docs\sub\c.txt"
        ];

        string result = PathUtils.GetCommonFolder(paths);

        Assert.That(result, Is.EqualTo(@"C:\Users\docs"));
    }

    [Test]
    public void GetCommonFolder_NoCommon_ReturnsEmpty()
    {
        List<string> paths =
        [
            @"C:\a\file.txt",
            @"D:\b\file.txt"
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

    #endregion

    #region GetDifferingRoot

    [Test]
    public void GetDifferingRoot_Normal()
    {
        string path1 = @"a\path\to\a\file.txt";
        string path2 = @"another\root\that goes\to\a\file.txt";

        (string result1, string result2) = PathUtils.GetDifferingRoot(path1, path2);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.EqualTo(@"a\path"));
            Assert.That(result2, Is.EqualTo(@"another\root\that goes"));
        });
    }

    [Test]
    public void GetDifferingRoot_Same()
    {
        string path1 = @"a\path\to\a\file.txt";

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
        string path1 = @"a\path\to\a\file.txt";
        string path2 = @"a\file.txt";

        (string result1, string result2) = PathUtils.GetDifferingRoot(path1, path2);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.EqualTo(@"a\path\to"));
            Assert.That(result2, Is.EqualTo(""));
        });
    }

    [Test]
    public void GetDifferingRoot_NullPath1_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PathUtils.GetDifferingRoot(null!, @"a\path\file.txt"));
    }

    [Test]
    public void GetDifferingRoot_NullPath2_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PathUtils.GetDifferingRoot(@"a\path\file.txt", null!));
    }

    [Test]
    public void GetDifferingRoot_NoCommonEnd_ReturnsBothPathsUnchanged()
    {
        string path1 = @"root1\file.txt";
        string path2 = @"root2\other.txt";

        (string result1, string result2) = PathUtils.GetDifferingRoot(path1, path2);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.EqualTo(path1));
            Assert.That(result2, Is.EqualTo(path2));
        });
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
        Assert.That(PathUtils.MatchesAnyGlob(@"docs\readme.md", ["**/*.md"]), Is.True);
    }

    [Test]
    public void MatchesAnyGlob_NoMatch_ReturnsFalse()
    {
        Assert.That(PathUtils.MatchesAnyGlob(@"docs\file.txt", ["*.cs"]), Is.False);
    }

    [Test]
    public void MatchesAnyGlob_EmptyGlobList_ReturnsFalse()
    {
        Assert.That(PathUtils.MatchesAnyGlob(@"docs\file.txt", []), Is.False);
    }

    [Test]
    public void MatchesAnyGlob_MultipleGlobs_MatchesSecond()
    {
        Assert.That(PathUtils.MatchesAnyGlob(@"docs\file.txt", ["*.cs", "**/*.txt"]), Is.True);
    }

    [Test]
    public void MatchesAnyGlob_MultipleGlobs_NoneMatch_ReturnsFalse()
    {
        Assert.That(PathUtils.MatchesAnyGlob(@"docs\file.txt", ["*.cs", "*.md"]), Is.False);
    }

    [Test]
    public void MatchesAnyGlob_MultipleGlobs_ExactMatchAmongPatterns()
    {
        Assert.That(PathUtils.MatchesAnyGlob(@"docs\file.txt", ["*.cs", @"docs\file.txt"]), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    public void MatchesAnyGlob_NullOrEmptyPath_ReturnsFalse(string? path)
    {
        Assert.That(PathUtils.MatchesAnyGlob(path!, ["*"]), Is.False);
    }

    #endregion
}
