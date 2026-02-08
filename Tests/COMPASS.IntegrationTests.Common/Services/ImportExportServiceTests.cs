using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Platform.Storage;
using COMPASS.Common.Interfaces.Repos;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Infra.Tools;
using COMPASS.Tests.Common.DataGenerators;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System.Text.Json;
using Constants = COMPASS.Common.Models.Constants;

namespace COMPASS.IntegrationTests.Common.Services
{
    [TestFixture]
    public class ImportExportServiceTests
    {
        [Test]
        public async Task OpenSatchel()
        {
            //Create info with a version higher than the current one
            SatchelInfo info = new()
            {
                MinCodexInfoVersion = "20.15.0",
                MinTagsVersion = "1.0.0",
            };

            var storageService = ServiceResolver.Resolve<IImportExportService>();
            var repo = ServiceResolver.ResolveKeyed<ICodexCollectionRepository>(StorageStrategy.Xml);

            using var zip = ZipArchive.Create();
            zip.AddEntry(Constants.SatchelInfoFileName, GenerateStreamFromString(JsonSerializer.Serialize(info)));
            zip.AddEntry("Tags.xml", GenerateStreamFromString("pseudo data"));

            string path = Path.GetTempPath() + Guid.NewGuid() + Constants.SatchelExtension;
            await zip.SaveToAsync(path, CompressionType.None);

            //Because satchel does not contain a codexInfo file, should work
            var collectionId = await storageService.OpenSatchel(path);
            Assert.That(collectionId, Is.Not.Null);
            repo.DeleteCollection(collectionId);

            //Now add a codex file
            zip.AddEntry("CodexInfo.xml", GenerateStreamFromString("Not important"));
            await zip.SaveToAsync(path, CompressionType.None);

            //Now that the codex file is added, should be null
            collectionId = await storageService.OpenSatchel(path);
            Assert.That(collectionId, Is.Null);

            File.Delete(path);
        }

        [AvaloniaTest]
        public async Task ExportCollection()
        {
            var window = new Window();
            window.Show();

            //Setup
            string testCollectionId = "__SatchelExport";
            var testCollection = CollectionGenerator.GetCompleteCollection(testCollectionId);
            var importExportService = ServiceResolver.Resolve<IImportExportService>();

            //Export
            var filePath = Path.GetTempPath() + Guid.NewGuid().ToString() + Constants.SatchelExtension;
            var targetFile = await GetStorageFileFromPath(filePath, window);
            Assert.That(targetFile, Is.Not.Null, "Could not get target file for satchel export");
            await importExportService.ExportCollection(testCollection, targetFile, false, false);
            await Task.Delay(100);

            //Assert export succesfull
            var fileInfo = new FileInfo(filePath);
            Assert.That(fileInfo.Length, Is.GreaterThan(0), "Exported satchel file is empty");

            CodexCollectionVM? deserializedCollectionVm = null;
            CodexCollectionVM? importedCollection = null;
            try
            {
                //Deserialize Satchel
                string? deserializedCollectionId = await importExportService.OpenSatchel(filePath);
                Assert.That(!string.IsNullOrEmpty(deserializedCollectionId));
                var deserializedCollection = new CodexCollection(deserializedCollectionId!);
                deserializedCollectionVm = new CodexCollectionVM(deserializedCollectionId!, deserializedCollection, StorageStrategy.Xml);
                await Task.Delay(100);
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
                Assert.That(importedCollection, Is.Not.Null, "Imported collection not found after import");
                Assert.That(importedCollection.Collection.AllCodices, Has.Count.EqualTo(deserializedCollection.AllCodices.Count));
                Assert.That(importedCollection.Collection.AllTags, Has.Count.EqualTo(deserializedCollection.AllTags.Count));
            }
            finally
            {
                //Cleanup
                importedCollection?.DeleteCollection();
                deserializedCollectionVm?.DeleteCollection();
                File.Delete(filePath);
                window.Close();
            }
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static async Task<IStorageFile> GetStorageFileFromPath(string path, Window window)
        {
            IStorageProvider storageProvider = window.StorageProvider;

            File.Create(path).Close();
            Assert.That(File.Exists(path));
            var storagefile = await storageProvider.TryGetFileFromPathAsync(path);

            return storagefile!;
        }
    }
}
