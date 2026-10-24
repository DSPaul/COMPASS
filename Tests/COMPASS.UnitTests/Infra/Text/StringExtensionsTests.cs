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

    #region GetValidVariant

    [Test]
    public void GetValidVariant_AlreadyValid_ReturnsSame()
    {
        string result = "collection".GetValidVariant(_ => true);

        Assert.That(result, Is.EqualTo("collection"));
    }

    [Test]
    public void GetValidVariant_FirstCandidateInvalid_AppendsOne()
    {
        string result = "collection".GetValidVariant(name => name != "collection");

        Assert.That(result, Is.EqualTo("collection (1)"));
    }

    [Test]
    public void GetValidVariant_MultipleCandidatesInvalid_DoesNotCompoundSuffixes()
    {
        // "collection" and "collection (1)" are taken, so the next free name is "collection (2)"
        string result = "collection".GetValidVariant(name => name is not ("collection" or "collection (1)"));

        Assert.That(result, Is.EqualTo("collection (2)"));
    }

    [Test]
    public void GetValidVariant_NoValidVariant_ReturnsOriginalAfterMaxAttempts()
    {
        const int maxAttempts = 5;
        int attempts = 0;

        // A name rejected for being too long: appending " (n)" can never make it valid
        string longName = new('a', 130);

        string result = longName.GetValidVariant(name =>
        {
            attempts++;
            return name.Length <= 127;
        }, maxAttempts);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(longName));
            Assert.That(attempts, Is.EqualTo(maxAttempts + 1)); // initial check + one per attempt
        });
    }

    #endregion
}
