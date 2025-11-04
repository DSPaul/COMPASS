using Avalonia.Headless.NUnit;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Sources;
using COMPASS.Common.ViewModels.Main;

namespace Tests.Sources
{
    [TestFixture]
    public class HomeBrewery
    {
        const string TEST_COLLECTION = "__unitTests";
        private const string TEST_URL = @"https://homebrewery.naturalcrit.com/share/FegJIEB2KUUo";

        [OneTimeSetUp]
        public void Init()
        {
            _ = new MainViewModel();
            if (!CollectionManager.CollectionNames.Contains(TEST_COLLECTION))
            {
                using var handle = CollectionManager.CreateAndLoadCollection(TEST_COLLECTION).Result;
            }
        }

        [AvaloniaTest]
        public async Task GetMetaDataFromHomeBrewery()
        {
            SourceSet sources = new()
            {
                SourceURL = TEST_URL
            };

            var source = MetaDataSource.GetSource(MetaDataSourceType.Homebrewery, new CodexCollection("TEST_COLLECTION"));

            SourceMetaData response = await source!.GetMetaData(sources);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.Not.Null);
                Assert.That(string.IsNullOrEmpty(response.Title), Is.False);
                Assert.That(string.IsNullOrEmpty(response.Description), Is.False);
                Assert.That(response.ReleaseDate, Is.Not.Null);
                Assert.That(response.PageCount, Is.EqualTo(1));
            });
        }

        [AvaloniaTest, Order(2)]
        public async Task GetCoverFromHomeBrewery()
        {
            //Setup
            var source = MetaDataSource.GetSource(MetaDataSourceType.Homebrewery, new CodexCollection("TEST_COLLECTION"));
            var sources = new SourceSet()
            {
                SourceURL = TEST_URL
            };

            //Fetch the cover
            var cover = await source!.FetchCover(sources);

            //see if it worked
            Assert.That(cover, Is.Not.Null, "Failed to fetch cover");
        }
    }
}
