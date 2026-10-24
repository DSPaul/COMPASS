using Autofac;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Platform.Storage;
using COMPASS.Common.Domain;
using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Covers;
using COMPASS.Common.Features.ImportExport;
using COMPASS.Common.Features.Persistence;
using COMPASS.Common.Features.Persistence.Repositories;
using COMPASS.Common.Features.Tabs;
using COMPASS.Tests.Common.DataGenerators;
using COMPASS.Tests.Common.Mocks;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers.Zip;
using System.Text.Json;
using Constants = COMPASS.Common.Domain.Constants;

namespace COMPASS.IntegrationTests.Common.Services
{
    [TestFixture]
    public class ImportExportServiceTests
    {
        private IContainer _container = null!;
        private CollectionHandle _initialCollection = null!;

        private IContainer BuildContainer() => Helpers.SetupContainer(builder =>
        {
            builder.RegisterType<ImportExportService>().As<IImportExportService>();
            //Importing a collection regenerates its covers; keep that off the network
            builder.RegisterType<MockCoverService>().As<ICoverService>();
        });

        [SetUp]
        public void SetUp()
        {
            _container = BuildContainer();

            //The app always has at least one collection: it creates the default one when there are none.
            //The import wizard needs an existing collection to target, so set it up the same way the app does.
            _initialCollection = _container.Resolve<CollectionManager>().GetOrCreateInitialCollectionVM();
        }

        [TearDown]
        public void TearDown()
        {
            //The import wizard opens a tab for the collection it imported, and an open tab keeps a
            //collection from being deleted, so close the tabs before deleting the test collections
            TabsViewModel tabsViewModel = _container.Resolve<TabsViewModel>();
            while (tabsViewModel.Tabs.Count > 0)
            {
                CollectionTabVM tab = tabsViewModel.Tabs[^1];
                tabsViewModel.Tabs.Remove(tab);
                tab.Dispose();
            }

            _initialCollection.Dispose();

            foreach (CodexCollectionVM collectionVm in _container.Resolve<CollectionManager>().CollectionVms.ToList())
            {
                collectionVm.DeleteCollection();
            }

            _container.Dispose();
        }

        [Test]
        public async Task OpenSatchel()
        {
            //Create info with a version higher than the current one
            SatchelInfo info = new("1.0.0")
            {
                MinCodexInfoVersion = "20.15.0",
                MinTagsVersion = "1.0.0",
            };

            string path = Path.GetTempPath() + Guid.NewGuid() + Constants.SatchelExtension;
            var storageService = _container.Resolve<IImportExportService>();
            var repo = _container.ResolveKeyed<ICodexCollectionRepository>(StorageStrategy.Xml);
            var satchelLogger = _container.Resolve<MockLogger>();

            await using (var zip = await ZipArchive.CreateAsyncArchive())
            {
                await zip.AddEntryAsync(Constants.SatchelInfoFileName, GenerateStreamFromString(JsonSerializer.Serialize(info)));
                await zip.AddEntryAsync("Tags.xml", GenerateStreamFromString("<?xml version=\"1.0\" encoding=\"utf-8\"?><ArrayOfTag />"));

                await using var fileStream1 = File.Create(path);
                await zip.SaveToAsync(fileStream1, new ZipWriterOptions(CompressionType.None));
            }

            //Because satchel does not contain a codexInfo file, should work
            var collection = await storageService.OpenSatchel(path);
            Assert.That(collection, Is.Not.Null);

            Assert.That(satchelLogger.Errors, Is.Empty, "OpenSatchel logged errors on happy path");
            Assert.That(satchelLogger.Warnings, Is.Empty, "OpenSatchel logged warnings on happy path");
            Assert.That(satchelLogger.Fatals, Is.Empty, "OpenSatchel logged fatals on happy path");
            repo.DeleteCollection(collection.Name);

            //Now add a codex file
            await using (var zip = await ZipArchive.CreateAsyncArchive())
            {
                await zip.AddEntryAsync(Constants.SatchelInfoFileName, GenerateStreamFromString(JsonSerializer.Serialize(info)));
                await zip.AddEntryAsync("Tags.xml", GenerateStreamFromString("<?xml version=\"1.0\" encoding=\"utf-8\"?><ArrayOfTag />"));
                await zip.AddEntryAsync("CodexInfo.xml", GenerateStreamFromString("Not important"));

                await using var fileStream2 = File.Create(path);
                await zip.SaveToAsync(fileStream2, new ZipWriterOptions(CompressionType.None));
            }

            //Now that the codex file is added, should be null
            collection = await storageService.OpenSatchel(path);
            Assert.That(collection, Is.Null);

            Assert.That(satchelLogger.Warnings, Has.Count.EqualTo(1), "Version rejection should log exactly one warning");
            Assert.That(satchelLogger.Warnings[0], Does.Contain("incompatible"), "Version rejection warning should explain incompatibility");
            Assert.That(satchelLogger.Errors, Is.Empty, "Version rejection should not log errors");
            Assert.That(satchelLogger.Fatals, Is.Empty, "Version rejection should not log fatals");

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
            var importExportService = _container.Resolve<IImportExportService>();
            var exportLogger = _container.Resolve<MockLogger>();

            //Export
            var filePath = Path.GetTempPath() + Guid.NewGuid().ToString() + Constants.SatchelExtension;
            var targetFile = await GetStorageFileFromPath(filePath, window);
            Assert.That(targetFile, Is.Not.Null, "Could not get target file for satchel export");
            await importExportService.ExportCollection(testCollection, targetFile, false, false);
            await Task.Delay(100);

            //Assert export succesfull
            var fileInfo = new FileInfo(filePath);
            Assert.That(fileInfo.Length, Is.GreaterThan(0), "Exported satchel file is empty");

            Assert.That(exportLogger.Errors, Is.Empty, "ExportCollection logged errors");
            Assert.That(exportLogger.Warnings, Is.Empty, "ExportCollection logged warnings");
            Assert.That(exportLogger.Fatals, Is.Empty, "ExportCollection logged fatals");

            CodexCollectionVM? deserializedCollectionVm = null;
            ImportCollectionViewModel? importViewModel = null;
            try
            {
                //Deserialize Satchel
                var deserializedCollection = await importExportService.OpenSatchel(filePath);
                Assert.That(deserializedCollection, Is.Not.Null);

                Assert.That(exportLogger.Errors, Is.Empty, "OpenSatchel of exported collection logged errors");
                Assert.That(exportLogger.Warnings, Is.Empty, "OpenSatchel of exported collection logged warnings");
                Assert.That(exportLogger.Fatals, Is.Empty, "OpenSatchel of exported collection logged fatals");
                deserializedCollectionVm = _container.Resolve<CodexCollectionVMFactory>().Create(deserializedCollection, StorageStrategy.Xml);
                importViewModel = _container.Resolve<ImportCollectionViewModelFactory>().Create(deserializedCollectionVm);

                Assert.That(importViewModel.ContentSelectorVM.HasCodices, "deserialized satchel has no Codices");
                Assert.That(importViewModel.ContentSelectorVM.HasTags, "deserialized satchel has no Tags");
                Assert.That(deserializedCollection.AllCodices, Has.Count.EqualTo(testCollection.AllCodices.Count));
                Assert.That(deserializedCollection.AllTags, Has.Count.EqualTo(testCollection.AllTags.Count));

                //Complete import to new Collection
                importViewModel.CollectionName = "Imported_Satchel"; //cannot use a protect __ name because it is an illegal name
                importViewModel.MergeIntoCollection = false;
                await importViewModel.Finish();

                CodexCollectionVM? importedCollection = _container.Resolve<CollectionManager>().GetCollectionVM("Imported_Satchel");
                Assert.That(importedCollection, Is.Not.Null, "Imported collection not found after import");
                Assert.That(importedCollection.Collection.AllCodices, Has.Count.EqualTo(deserializedCollection.AllCodices.Count));
                Assert.That(importedCollection.Collection.AllTags, Has.Count.EqualTo(deserializedCollection.AllTags.Count));

                Assert.That(exportLogger.Errors, Is.Empty, "Import Finish logged errors");
                Assert.That(exportLogger.Warnings, Is.Empty, "Import Finish logged warnings");
                Assert.That(exportLogger.Fatals, Is.Empty, "Import Finish logged fatals");
            }
            finally
            {
                //Cleanup: the imported collection and the tabs are cleaned up in TearDown
                importViewModel?.Dispose();
                deserializedCollectionVm?.DeleteCollection();
                File.Delete(filePath);
                window.Close();
            }
        }

        [AvaloniaTest]
        public async Task ImportCollection_WithUserFiles_RoundTripsFiles()
        {
            var window = new Window();
            window.Show();

            //Setup: a collection whose codices point at real files, one of them nested in a subfolder
            string sourceDir = Path.Combine(Path.GetTempPath(), "COMPASS_ImportFiles_" + Guid.NewGuid());
            string nestedDir = Path.Combine(sourceDir, "books");
            Directory.CreateDirectory(nestedDir);
            string fileA = Path.Combine(sourceDir, "a.pdf");
            string fileB = Path.Combine(nestedDir, "b.pdf");
            await File.WriteAllTextAsync(fileA, "content-a");
            await File.WriteAllTextAsync(fileB, "content-b");

            var testCollection = new CodexCollection("__SatchelUserFiles");
            testCollection.AllCodices.Add(new Codex(testCollection)
            {
                Id = 1,
                Title = "A",
                Sources = new SourceSet { Path = fileA }
            });
            testCollection.AllCodices.Add(new Codex(testCollection)
            {
                Id = 2,
                Title = "B",
                Sources = new SourceSet { Path = fileB }
            });

            string satchelPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Constants.SatchelExtension);
            ImportCollectionViewModel? importViewModel = null;
            try
            {
                var importExportService = _container.Resolve<IImportExportService>();

                //Export including the user files
                var targetFile = await GetStorageFileFromPath(satchelPath, window);
                Assert.That(targetFile, Is.Not.Null, "Could not get target file for satchel export");
                await importExportService.ExportCollection(testCollection, targetFile, includeFiles: true, includeCovers: false);
                await Task.Delay(100);

                //Import
                var deserialized = await importExportService.OpenSatchel(satchelPath);
                Assert.That(deserialized, Is.Not.Null, "Could not read the exported satchel");

                //Paths stored in a satchel must be relative and use portable separators, so the
                //satchel can be imported on a different platform than it was exported from
                foreach (Codex codex in deserialized!.AllCodices)
                {
                    Assert.That(Path.IsPathFullyQualified(codex.Sources.Path), Is.False,
                        $"Satchel stored an absolute path: {codex.Sources.Path}");
                    Assert.That(codex.Sources.Path.Contains('\\'), Is.False,
                        $"Satchel stored a non-portable path: {codex.Sources.Path}");
                }

                var deserializedCollectionVm = _container.Resolve<CodexCollectionVMFactory>().Create(deserialized, StorageStrategy.Xml);
                importViewModel = _container.Resolve<ImportCollectionViewModelFactory>().Create(deserializedCollectionVm);
                importViewModel.CollectionName = "Imported_UserFiles";
                importViewModel.MergeIntoCollection = false;
                await importViewModel.Finish();

                CodexCollectionVM? importedCollection = _container.Resolve<CollectionManager>().GetCollectionVM("Imported_UserFiles");
                Assert.That(importedCollection, Is.Not.Null, "Imported collection not found after import");
                Assert.That(importedCollection.Collection.AllCodices, Has.Count.EqualTo(2));

                var importedCodices = importedCollection.Collection.AllCodices.OrderBy(codex => codex.Id).ToList();
                string expectedFilesDir = Path.Combine("Collections", "Imported_UserFiles", Constants.DIR_FILES);

                Assert.Multiple(() =>
                {
                    Assert.That(Path.GetFileName(importedCodices[0].Sources.Path), Is.EqualTo("a.pdf"));
                    Assert.That(Path.GetFileName(importedCodices[1].Sources.Path), Is.EqualTo("b.pdf"));

                    //The files must have been moved into the imported collection's user files folder
                    Assert.That(importedCodices[0].Sources.Path, Does.Contain(expectedFilesDir));
                    Assert.That(importedCodices[1].Sources.Path, Does.Contain(expectedFilesDir));

                    //...and actually exist there with the original content
                    Assert.That(File.Exists(importedCodices[0].Sources.Path), Is.True,
                        $"Imported file is missing at {importedCodices[0].Sources.Path}");
                    Assert.That(File.Exists(importedCodices[1].Sources.Path), Is.True,
                        $"Imported file is missing at {importedCodices[1].Sources.Path}");
                    Assert.That(File.ReadAllText(importedCodices[0].Sources.Path), Is.EqualTo("content-a"));
                    Assert.That(File.ReadAllText(importedCodices[1].Sources.Path), Is.EqualTo("content-b"));
                });
            }
            finally
            {
                //Cleanup: the imported collection and the tabs are cleaned up in TearDown
                importViewModel?.Dispose();
                if (File.Exists(satchelPath)) File.Delete(satchelPath);
                if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
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
