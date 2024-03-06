using COMPASS.Models;
using COMPASS.Services;
using COMPASS.ViewModels;
using COMPASS.ViewModels.Import;
using Tests.DataGenerators;

namespace Tests.IntegrationTests
{
    [TestClass]
    public class Satchels_Test
    {
        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            MainViewModel mvm = new();
        }

        [TestMethod]
        public async Task TestSatchelExportImport()
        {
            //Setup
            var testCollection = CollectionGenerator.GetCompleteCollection("__SatchelExport");
            MainViewModel.CollectionVM.DeleteCollection(testCollection); //make sure to start with a clean slate
            testCollection.Save();

            //Export
            var filePath = Path.GetTempPath() + Guid.NewGuid().ToString() + Constants.SatchelExtension;
            ExportCollectionViewModel exportViewModel = new(testCollection);
            await exportViewModel.ApplyChoices();
            await exportViewModel.ExportToFile(filePath);

            //Assert export succesfull
            Assert.IsTrue(File.Exists(filePath));

            CodexCollection? importedCollection = null;
            try
            {
                //Wipe to collection from COMPASS
                MainViewModel.CollectionVM.DeleteCollection(testCollection);

                //Import
                importedCollection = await IOService.OpenSatchel(filePath);
                Assert.IsNotNull(importedCollection);
                ImportCollectionViewModel importViewModel = new(importedCollection);

                Assert.IsTrue(importViewModel.ContentSelectorVM.HasCodices, "imported Collection has no Codices");
                Assert.IsTrue(importViewModel.ContentSelectorVM.HasTags, "imported Collection has no Tags");
                Assert.AreEqual(testCollection.AllCodices.Count, importedCollection.AllCodices.Count);
                Assert.AreEqual(testCollection.AllTags.Count, importedCollection.AllTags.Count);
            }
            finally
            {
                //Cleanup
                MainViewModel.CollectionVM.DeleteCollection(testCollection);
                if (importedCollection != null)
                {
                    MainViewModel.CollectionVM.DeleteCollection(importedCollection);
                }
                File.Delete(filePath);
            }
        }
    }
}
