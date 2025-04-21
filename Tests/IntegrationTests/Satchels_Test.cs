using Avalonia.Headless.NUnit;
using COMPASS.Common.Models;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Import;
using Tests.DataGenerators;

namespace Tests.IntegrationTests
{
    [TestFixture]
    public class Satchels_Test
    {
        [OneTimeSetUp]
        public void Init() => _ = new MainViewModel();

        [AvaloniaTest]
        public async Task TestSatchelExportImport()
        {
            // //Setup
            // var testCollection = CollectionGenerator.GetCompleteCollection("__SatchelExport");
            // MainViewModel.CollectionVM.DeleteCollection(testCollection); //make sure to start with a clean slate
            // testCollection.Save();
            //
            // //Export
            // var filePath = Path.GetTempPath() + Guid.NewGuid().ToString() + Constants.SatchelExtension;
            // ExportCollectionViewModel exportViewModel = new(testCollection);
            // exportViewModel.ApplyChoices();
            // await exportViewModel.ExportToFile(filePath);
            //
            // //Assert export succesfull
            // Assert.IsTrue(File.Exists(filePath));
            // Thread.Sleep(100);
            //
            // CodexCollection? deserializedCollection = null;
            // CodexCollection? importedCollection = null;
            // try
            // {
            //     //Wipe to collection from COMPASS
            //     MainViewModel.CollectionVM.DeleteCollection(testCollection);
            //
            //     //Deserialize Satchel
            //     deserializedCollection = await IOService.OpenSatchel(filePath);
            //     Assert.IsNotNull(deserializedCollection);
            //     Thread.Sleep(100);
            //     ImportCollectionViewModel importViewModel = new(deserializedCollection);
            //
            //     Assert.IsTrue(importViewModel.ContentSelectorVM.HasCodices, "deserialized satchel has no Codices");
            //     Assert.IsTrue(importViewModel.ContentSelectorVM.HasTags, "deserialized satchel has no Tags");
            //     Assert.AreEqual(testCollection.AllCodices.Count, deserializedCollection.AllCodices.Count);
            //     Assert.AreEqual(testCollection.AllTags.Count, deserializedCollection.AllTags.Count);
            //
            //     //Complete import to new Collection
            //     importViewModel.CollectionName = "Imported_Satchel"; //cannot use a protect __ name because it is an illegal name
            //     importViewModel.MergeIntoCollection = false;
            //     await importViewModel.Finish();
            //
            //     importedCollection = MainViewModel.CollectionVM.CurrentCollection;
            //
            //     Assert.AreEqual(deserializedCollection.AllCodices.Count, importedCollection.AllCodices.Count);
            //     Assert.AreEqual(deserializedCollection.AllTags.Count, importedCollection.AllTags.Count);
            // }
            // finally
            // {
            //     //Cleanup
            //     MainViewModel.CollectionVM.DeleteCollection(testCollection);
            //     if (importedCollection != null && importedCollection.DirectoryName == "Imported_Satchel") //make sure not to delete something else
            //     {
            //         MainViewModel.CollectionVM.DeleteCollection(importedCollection);
            //     }
            //     if (deserializedCollection != null && deserializedCollection.DirectoryName.StartsWith("__")) //make sure not to delete something else
            //     {
            //         MainViewModel.CollectionVM.DeleteCollection(deserializedCollection);
            //     }
            //     File.Delete(filePath);
            // }
        }
    }
}
