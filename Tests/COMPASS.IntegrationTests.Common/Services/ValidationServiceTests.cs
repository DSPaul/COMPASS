using COMPASS.Common.Services;

namespace COMPASS.IntegrationTests.Common.Services;

[TestFixture]
public class ValidationServiceTests
{
    [Test]
    [TestCase("0306406152", true)]    // Valid ISBN-10
    [TestCase("0451526538", true)]    // Valid ISBN-10 (1984)
    [TestCase("007462542X", true)]    // Valid ISBN-10 with X check digit
    [TestCase("0306406151", false)]   // Invalid ISBN-10 (bad check digit)
    [TestCase("123456789", false)]    // Too short
    [TestCase("", false)]             // Empty
    public void IsValidISBN_ISBN10_ReturnsExpectedResult(string isbn, bool expected)
    {
        bool result = ValidationService.IsValidISBN(isbn);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase("9780306406157", true)]   // Valid ISBN-13
    [TestCase("9780140449136", true)]   // Valid ISBN-13 (The Republic)
    [TestCase("9780306406158", false)]  // Invalid ISBN-13 (bad check digit)
    [TestCase("978030640615", false)]   // Too short for ISBN-13
    [TestCase("97803064061578", false)] // Too long
    public void IsValidISBN_ISBN13_ReturnsExpectedResult(string isbn, bool expected)
    {
        bool result = ValidationService.IsValidISBN(isbn);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void IsValidISBN_NonNumericCharacters_ReturnsFalse()
    {
        bool result = ValidationService.IsValidISBN("abcdefghij");

        Assert.That(result, Is.False);
    }
}
