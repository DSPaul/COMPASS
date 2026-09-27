using COMPASS.Infra.Text;

namespace COMPASS.UnitTests.Infra.Text;

[TestFixture]
public class StringExtensionsTests
{
    #region PadNumbers

    [Test]
    public void PadNumbers_PadsEmbeddedNumbers()
    {
        string result = "file3name".PadNumbers(4);

        Assert.That(result, Is.EqualTo("file0003name"));
    }

    [Test]
    public void PadNumbers_MultipleNumbers()
    {
        string result = "ch1pg25".PadNumbers(4);

        Assert.That(result, Is.EqualTo("ch0001pg0025"));
    }

    [Test]
    public void PadNumbers_NullOrEmpty_ReturnsSame()
    {
        Assert.Multiple(() =>
        {
            Assert.That(((string?)null!).PadNumbers(), Is.Null);
            Assert.That("".PadNumbers(), Is.EqualTo(""));
        });
    }

    [Test]
    public void PadNumbers_NoNumbers_ReturnsSame()
    {
        Assert.That("hello".PadNumbers(), Is.EqualTo("hello"));
    }

    #endregion

    #region RemoveDiacritics

    [Test]
    public void RemoveDiacritics_RemovesAccents()
    {
        Assert.That("héllo".RemoveDiacritics(), Is.EqualTo("hello"));
    }

    [Test]
    public void RemoveDiacritics_NoAccents_ReturnsSame()
    {
        Assert.That("hello".RemoveDiacritics(), Is.EqualTo("hello"));
    }

    [Test]
    public void RemoveDiacritics_MultipleAccents()
    {
        Assert.That("café résumé".RemoveDiacritics(), Is.EqualTo("cafe resume"));
    }

    #endregion
}
