using COMPASS.Common.Models;
using COMPASS.Common.Sources;
using COMPASS.Tests.Common.Mocks;

namespace COMPASS.UnitTests.Sources
{
    [TestFixture]
    public class OnlineMetadataSourceTests
    {
        private static GenericOnlineMetadataSource CreateGenericSource() =>
            new(new MockLogger(), new MockPreferencesService(), null!, null!);

        private static GmBinderMetadataSource CreateGmBinderSource() =>
            new(new MockLogger(), new MockPreferencesService(), null!, null!);

        private static SourceSet UrlSources(string url) => new() { SourceURL = url };

        [TestCase("https://example.com/page")]
        [TestCase("http://intranet.local/page")]
        [TestCase("HTTPS://EXAMPLE.COM/PAGE")]
        public void GenericSource_AcceptedSchemes_AreValid(string url)
        {
            var source = CreateGenericSource();

            Assert.That(source.IsValidSource(UrlSources(url)), Is.True);
        }

        [TestCase("ftp://example.com/file")]
        [TestCase("")]
        [TestCase("C:\\books\\sheet.pdf")]
        public void GenericSource_OtherInputs_AreInvalid(string url)
        {
            var source = CreateGenericSource();

            Assert.That(source.IsValidSource(UrlSources(url)), Is.False);
        }

        [Test]
        public void GenericSource_FileUrl_IsInvalid_ConvertedToPathByDialog()
        {
            //file:// inputs never reach the source: ImportURLViewModel converts them
            //to offline paths, so the source correctly rejects them here
            var source = CreateGenericSource();

            Assert.That(source.IsValidSource(UrlSources("file:///C:/pages/sheet.html")), Is.False);
        }

        [Test]
        public void SpecificSource_HttpsShareUrl_IsValid()
        {
            var source = CreateGmBinderSource();

            Assert.That(source.IsValidSource(UrlSources("https://www.gmbinder.com/share/abc123")), Is.True);
        }

        [Test]
        public void SpecificSource_UppercaseUrl_IsValid()
        {
            var source = CreateGmBinderSource();

            Assert.That(source.IsValidSource(UrlSources("HTTPS://WWW.GMBINDER.COM/SHARE/abc123")), Is.True);
        }

        [TestCase("http://www.gmbinder.com/share/abc123")]
        [TestCase("https://homebrewery.naturalcrit.com/share/abc123")]
        public void SpecificSource_WrongSchemeOrSite_IsInvalid(string url)
        {
            var source = CreateGmBinderSource();

            Assert.That(source.IsValidSource(UrlSources(url)), Is.False);
        }
    }
}
