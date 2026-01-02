using Avalonia.Headless.NUnit;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Infra.Tools;
using COMPASS.Tests.Common.DataGenerators;

namespace COMPASS.IntegrationTests.Common.ViewModels
{
    [TestFixture]
    public class Satchels_Test
    {
        [AvaloniaTest]
        public async Task TestSatchelExportImport()
        {
            //Setup
            string testCollectionId = "__SatchelExport";
            var testCollection = CollectionGenerator.GetCompleteCollection(testCollectionId);

            //Export
            var filePath = Path.GetTempPath() + Guid.NewGuid().ToString() + Constants.SatchelExtension;
            ExportCollectionViewModel exportViewModel = new(testCollection);
            exportViewModel.ApplyChoices();
            await exportViewModel.ExportToFile(filePath);

            //Assert export succesfull
            Assert.That(File.Exists(filePath));
            Thread.Sleep(100);

            CodexCollectionVM? deserializedCollectionVm = null;
            CodexCollectionVM? importedCollection = null;
            try
            {
                //Deserialize Satchel
                var xmlStorageService = ServiceResolver.ResolveKeyed<ICodexCollectionStorageService>(StorageStrategy.Xml);
                string? deserializedCollectionId = await xmlStorageService.OpenSatchel(filePath);
                Assert.That(!string.IsNullOrEmpty(deserializedCollectionId));
                var deserializedCollection = new CodexCollection(deserializedCollectionId!);
                deserializedCollectionVm = new CodexCollectionVM(deserializedCollectionId!, deserializedCollection, xmlStorageService);
                Thread.Sleep(100);
                ImportCollectionViewModel importViewModel = new(deserializedCollectionVm);

                Assert.That(importViewModel.ContentSelectorVM.HasCodices, "deserialized satchel has no Codices");
                Assert.That(importViewModel.ContentSelectorVM.HasTags, "deserialized satchel has no Tags");
                Assert.That(deserializedCollection.AllCodices, Has.Count.EqualTo(testCollection.AllCodices.Count));
                Assert.That(deserializedCollection.AllTags, Has.Count.EqualTo(testCollection.AllTags.Count));

                //Complete import to new Collection
                importViewModel.CollectionName = "Imported_Satchel"; //cannot use a protect __ name because it is an illegal name
                importViewModel.MergeIntoCollection = false;
                await importViewModel.Finish();

                importedCollection = CollectionManager.GetCollectionVM("Imported_Satchel");
                Assert.That(importedCollection , Is.Not.Null, "Imported collection not found after import");
                Assert.That(importedCollection.Collection.AllCodices, Has.Count.EqualTo(deserializedCollection.AllCodices.Count));
                Assert.That(importedCollection.Collection.AllTags, Has.Count.EqualTo(deserializedCollection.AllTags.Count));
            }
            finally
            {
                //Cleanup
                importedCollection?.DeleteCollection();
                deserializedCollectionVm?.DeleteCollection();
                File.Delete(filePath);
            }
        }
    }
}
