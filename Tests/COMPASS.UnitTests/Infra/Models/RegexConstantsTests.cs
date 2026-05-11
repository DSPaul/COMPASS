using COMPASS.Infra.Models;

namespace COMPASS.UnitTests.Infra.Models;

[TestFixture]
public class RegexConstantsTests
{
    [TestCase("9780306406157", true)]
    [TestCase("978-0-306-40615-7", true)]
    [TestCase("979 0 306 40615 7", true)]
    [TestCase("1234567890", false)]
    [TestCase("hello", false)]
    public void ISBN_MatchesCorrectly(string input, bool shouldMatch)
    {
        Assert.That(RegexConstants.ISBN().IsMatch(input), Is.EqualTo(shouldMatch));
    }

    [TestCase("hello world", "hello world")]
    [TestCase("hello   world", "hello world")]
    [TestCase("  leading", " leading")]
    [TestCase("trailing  ", "trailing ")]
    public void Whitespace_ReplacesMultipleSpaces(string input, string expected)
    {
        string result = RegexConstants.Whitespace().Replace(input, " ");

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("abc123def", true)]
    [TestCase("hello", false)]
    [TestCase("42", true)]
    public void NumbersOnly_MatchesDigits(string input, bool shouldMatch)
    {
        Assert.That(RegexConstants.Numbers().IsMatch(input), Is.EqualTo(shouldMatch));
    }
}
