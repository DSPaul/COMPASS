using COMPASS.Common.Features.ISBN;

namespace COMPASS.UnitTests.Features.ISBN;

[TestFixture]
public class ISBNPatternsTests
{
    [TestCase("9780306406157", true)]
    [TestCase("978-0-306-40615-7", true)]
    [TestCase("979 0 306 40615 7", true)]
    [TestCase("1234567890", false)]
    [TestCase("hello", false)]
    public void ISBN_MatchesCorrectly(string input, bool shouldMatch)
    {
        Assert.That(Patterns.ISBN().IsMatch(input), Is.EqualTo(shouldMatch));
    }
}
