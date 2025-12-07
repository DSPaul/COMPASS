using System.Text.Json;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Infra.Tools;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

namespace COMPASS.IntegrationTests.Common.Services
{
    [TestFixture]
    public class StorageServiceTests
    {
        [Test]
        public async Task StorageService_OpenSatchel()
        {
            //Create info with a version higher than the current one
            SatchelInfo info = new()
            {
                MinCodexInfoVersion = "20.0",
                MinTagsVersion = "1.0.0",
            };

            var storageService = ServiceResolver.ResolveKeyed<ICodexCollectionStorageService>(StorageStrategy.Xml);

            using var zip = ZipArchive.Create();
            zip.AddEntry(Constants.SatchelInfoFileName, GenerateStreamFromString(JsonSerializer.Serialize(info)));
            zip.AddEntry("Tags.xml", GenerateStreamFromString("pseudo data"));

            string path = Path.GetTempPath() + Guid.NewGuid().ToString() + Constants.SatchelExtension;
            await zip.SaveToAsync(path, CompressionType.None);

            //Because satchel does not contain a codexInfo file, should work
            var collectionId = await storageService.OpenSatchel(path);
            Assert.That(collectionId, Is.Not.Null);
            storageService.DeleteCollection(collectionId);

            //Now add a codex file
            zip.AddEntry("CodexInfo.xml", GenerateStreamFromString("Not important"));
            await zip.SaveToAsync(path, CompressionType.None);

            //Now that the codex file is added, should be null
            collectionId = await storageService.OpenSatchel(path);
            Assert.That(collectionId, Is.Null);

            File.Delete(path);
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
    }
}
