using Avalonia.Headless.NUnit;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Sources;

namespace Tests.Sources
{
    [TestFixture]
    public class HomeBrewery
    {
        const string TEST_COLLECTION = "__unitTests";

        [OneTimeSetUp]
        public void Init()
        {
            _ = new MainViewModel();
            if (!MainViewModel.CollectionVM.CollectionDirectories.Contains(TEST_COLLECTION))
            {
                MainViewModel.CollectionVM.CreateAndLoadCollection(TEST_COLLECTION);
            }
            else
            {
                MainViewModel.CollectionVM.CurrentCollection = new(TEST_COLLECTION);
                MainViewModel.CollectionVM.Refresh().Wait();
            }
        }

        [AvaloniaTest]
        public async Task GetMetaDataFromHomeBrewery()
        {
            SourceSet sources = new()
            {
                SourceURL = @"https://homebrewery.naturalcrit.com/share/FegJIEB2KUUo"
            };

            var vm = SourceViewModel.GetSourceVM(MetaDataSource.Homebrewery);

            CodexDto response = await vm!.GetMetaData(sources);

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
            var vm = SourceViewModel.GetSourceVM(MetaDataSource.Homebrewery);
            CodexCollection cc = new(TEST_COLLECTION);
            // cc.InitAsNew();
            // cc.Load();

            Codex codex = new(cc)
            {
                Sources = new()
                {
                    SourceURL = @"https://homebrewery.naturalcrit.com/share/FegJIEB2KUUo"
                }
            };

            //Clear existing data
            if (File.Exists(codex.CoverArtPath))
            {
                File.Delete(codex.CoverArtPath);
            }

            if (File.Exists(codex.ThumbnailPath))
            {
                File.Delete(codex.ThumbnailPath);
            }

            //Fetch the cover
            bool success = await vm!.FetchCover(codex);

            Assert.Multiple(() =>
            {
                //see if it worked
                Assert.That(success, Is.True, "Failed to fetch cover");
                Assert.That(File.Exists(codex.ThumbnailPath), Is.True, "Thumbnail doesn't exist");
                Assert.That(File.Exists(codex.CoverArtPath), Is.True, "Cover doesn't exist");
            });
        }
    }
}
