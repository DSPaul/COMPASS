using COMPASS.Infra.Tools;

namespace COMPASS.UnitTests.Infra.Tools;

[TestFixture]
public class FileFormatUtilsTests
{
    [TestCase(".png", true)]
    [TestCase(".jpg", true)]
    [TestCase(".jpeg", true)]
    [TestCase(".webp", true)]
    [TestCase(".PNG", true)]
    [TestCase(".pdf", false)]
    [TestCase(".txt", false)]
    public void IsImageFile_ReturnsExpected(string extension, bool expected)
    {
        string path = $"somefile{extension}";

        Assert.That(FileFormatUtils.IsImageFile(path), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    public void IsImageFile_NullOrEmpty_ReturnsFalse(string? path)
    {
        Assert.That(FileFormatUtils.IsImageFile(path!), Is.False);
    }

    [TestCase(".pdf", true)]
    [TestCase(".PDF", true)]
    [TestCase(".png", false)]
    [TestCase(".txt", false)]
    public void IsPDFFile_ReturnsExpected(string extension, bool expected)
    {
        string path = $"somefile{extension}";

        Assert.That(FileFormatUtils.IsPDFFile(path), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    public void IsPDFFile_NullOrEmpty_ReturnsFalse(string? path)
    {
        Assert.That(FileFormatUtils.IsPDFFile(path!), Is.False);
    }
}
