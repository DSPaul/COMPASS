using COMPASS.Infra.Text;

namespace COMPASS.UnitTests.Infra.Text;

[TestFixture]
public class RegexConstantsTests
{
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
