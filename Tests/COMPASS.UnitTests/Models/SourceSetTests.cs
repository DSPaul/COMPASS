using COMPASS.Common.Models;

namespace COMPASS.UnitTests.Models
{
    [TestFixture]
    public class SourceSetTests
    {
        [TestCase("google.com", "https://google.com")]
        [TestCase("google.com/page", "https://google.com/page")]
        [TestCase("www.foo.com/x", "https://www.foo.com/x")]
        [TestCase("WWW.FOO.COM", "https://WWW.FOO.COM")]
        [TestCase("localhost:8080/x", "https://localhost:8080/x")]
        public void SourceURL_BareDomain_GetsHttpsPrefix(string input, string expected)
        {
            var sources = new SourceSet { SourceURL = input };

            Assert.That(sources.SourceURL, Is.EqualTo(expected));
        }

        [TestCase("https://example.com/x")]
        [TestCase("http://intranet.local/x")]
        [TestCase("file:///C:/pages/x.html")]
        [TestCase("ftp://example.com/x")]
        public void SourceURL_WithScheme_LeftAlone(string input)
        {
            var sources = new SourceSet { SourceURL = input };

            Assert.That(sources.SourceURL, Is.EqualTo(input));
        }

        [TestCase(@"C:\books\x.pdf")]
        [TestCase("not a url")]
        [TestCase("")]
        public void SourceURL_NonUrl_LeftAlone(string input)
        {
            var sources = new SourceSet { SourceURL = input };

            Assert.That(sources.SourceURL, Is.EqualTo(input));
        }
    }
}
