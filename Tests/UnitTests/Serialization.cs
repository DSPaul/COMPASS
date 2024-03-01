using COMPASS.Models;
using COMPASS.Services;
using COMPASS.Tools;
using Ionic.Zip;
using System.Text.Json;

namespace Tests.UnitTests
{
    [TestClass]
    public class Serialization
    {
        [TestMethod]
        public void SerializeSatchelInfo()
        {
            var satchelInfo = new SatchelInfo();
            string json = JsonSerializer.Serialize(satchelInfo);

            Assert.IsFalse(String.IsNullOrEmpty(json));

            var newSatchelInfo = JsonSerializer.Deserialize<SatchelInfo>(json);
            Assert.IsNotNull(newSatchelInfo);
            Assert.IsTrue(newSatchelInfo.CreationVersion == Reflection.Version);
            Assert.IsTrue(newSatchelInfo.CreationDate < DateTime.Now);
        }

        [TestMethod]
        public void DeserializeSatchelInfoExtraFields()
        {
            //Check if new fields just get ignored as they should

            string json = @"{
                    ""CreationVersion"":""1.200.0"",
                    ""CreationDate"":""2024-02-22T20:08:07.1844264+01:00"",
                    ""MinCodexInfoVersion"":""1.156.0"",
                    ""MinTagsVersion"":""1.179.0"",
                    ""MinCollectionInfoVersion"":""1.100.0"",
                    ""Name"":""Paul""}";

            var satchelInfo = JsonSerializer.Deserialize<SatchelInfo>(json);
            Assert.IsNotNull(satchelInfo);
            Assert.IsTrue(satchelInfo.CreationVersion == "1.200.0");
        }

        [TestMethod]
        public void DeserializeIncompleteSatchelInfo()
        {
            //When incomplete, should fall back to defaults

            string json = @"{
                    ""CreationVersion"":""1.200.0"",
                    ""CreationDate"":""2024-02-22T20:08:07.1844264+01:00""}";

            var satchelInfo = JsonSerializer.Deserialize<SatchelInfo>(json);
            Assert.IsNotNull(satchelInfo);
            Assert.IsTrue(satchelInfo.MinTagsVersion == new SatchelInfo().MinTagsVersion);
        }

        [TestMethod]
        public async Task CheckSatchelInfoVersion()
        {
            //Create info with a version higher than the current one
            SatchelInfo info = new()
            {
                MinCodexInfoVersion = "20.0",
                MinTagsVersion = "1.0.0",
            };

            var zip = new ZipFile();
            zip.AddEntry(Constants.SatchelInfoFileName, JsonSerializer.Serialize(info));
            zip.AddEntry(Constants.TagsFileName, "pseudo data");

            var path = Path.GetTempPath() + Guid.NewGuid().ToString() + Constants.SatchelExtension;
            zip.Save(path);

            //Because satchel does not contain a codexInfo file, should work
            var collection = await IOService.OpenSatchel(path);
            Assert.IsNotNull(collection);
            Directory.Delete(collection.FullDataPath, true);

            //Now add a codex file
            zip.AddEntry(Constants.CodicesFileName, "Not important");
            zip.Save(path);

            //Now that the codex file is added, should be null
            collection = await IOService.OpenSatchel(path);
            Assert.IsNull(collection);

        }
    }
}
