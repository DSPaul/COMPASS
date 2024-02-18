using COMPASS.Models;
using COMPASS.ViewModels;
using COMPASS.ViewModels.Sources;

namespace COMPASS_Tests.Services
{
    [TestClass]
    public class HomeBrewery
    {
        static MainViewModel? mvm;
        const string TEST_COLLECTION = "__unitTests";

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            if (System.Windows.Application.Current == null)
                new System.Windows.Application();

            mvm = new MainViewModel();
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

        [TestMethod]
        public async Task GetMetaDataFromHomeBrewery()
        {
            Codex codex = new()
            {
                SourceURL = @"https://homebrewery.naturalcrit.com/share/FegJIEB2KUUo"
            };

            var vm = SourceViewModel.GetSourceVM(MetaDataSource.Homebrewery);

            Codex response = await vm.GetMetaData(codex);

            Assert.IsNotNull(response);
            Assert.IsFalse(String.IsNullOrEmpty(response.Title));
            Assert.IsFalse(String.IsNullOrEmpty(response.Description));
        }

        [TestMethod]
        public async Task GetCoverFromHomeBrewery()
        {
            //Setup
            var vm = SourceViewModel.GetSourceVM(MetaDataSource.Homebrewery);
            CodexCollection cc = new(TEST_COLLECTION);
            cc.Load();

            Codex codex = new(cc)
            {
                SourceURL = @"https://homebrewery.naturalcrit.com/share/FegJIEB2KUUo"
            };

            //Clear existing data
            if (File.Exists(codex.CoverArt))
            {
                File.Delete(codex.CoverArt);
            }

            if (File.Exists(codex.Thumbnail))
            {
                File.Delete(codex.Thumbnail);
            }

            //Fetch the cover
            bool success = await vm.FetchCover(codex);

            //see it it worked
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(codex.Thumbnail));
            Assert.IsTrue(File.Exists(codex.CoverArt));
        }
    }
}
