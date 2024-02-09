using COMPASS.Models;
using COMPASS.ViewModels;
using COMPASS.ViewModels.Sources;

namespace COMPASS_Tests.Services
{
    [TestClass]
    public class HomeBrewery
    {
        static MainViewModel? mvm;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            if (System.Windows.Application.Current == null)
                new System.Windows.Application();

            mvm = new MainViewModel();
            mvm.
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
    }
}
