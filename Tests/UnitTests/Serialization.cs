using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System.Text.Json;
using COMPASS.Common.Models;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;

namespace Tests.UnitTests
{
    [TestFixture]
    public class Serialization
    {
        [Test]
        public void SerializeSatchelInfo()
        {
            var satchelInfo = new SatchelInfo();
            string json = JsonSerializer.Serialize(satchelInfo);

            Assert.That(string.IsNullOrEmpty(json), Is.False);

            var newSatchelInfo = JsonSerializer.Deserialize<SatchelInfo>(json);
            Assert.That(newSatchelInfo, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(newSatchelInfo.CreationVersion, Is.EqualTo(Reflection.Version));
                Assert.That(newSatchelInfo.CreationDate, Is.LessThan(DateTime.Now));
            });
        }

        [Test]
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
            Assert.That(satchelInfo, Is.Not.Null);
            Assert.That(satchelInfo.CreationVersion, Is.EqualTo("1.200.0"));
        }

        [NUnit.Framework.Test]
        public void DeserializeIncompleteSatchelInfo()
        {
            //When incomplete, should fall back to defaults

            string json = @"{
                    ""CreationVersion"":""1.200.0"",
                    ""CreationDate"":""2024-02-22T20:08:07.1844264+01:00""}";

            var satchelInfo = JsonSerializer.Deserialize<SatchelInfo>(json);
            Assert.That(satchelInfo, Is.Not.Null);
            Assert.That(satchelInfo.MinTagsVersion, Is.EqualTo(new SatchelInfo().MinTagsVersion));
        }

        [Test]
        public async Task CheckSatchelInfoVersion()
        {
            //Create info with a version higher than the current one
            SatchelInfo info = new()
            {
                MinCodexInfoVersion = "20.0",
                MinTagsVersion = "1.0.0",
            };

            // using var zip = ZipArchive.Create();
            // zip.AddEntry(Constants.SatchelInfoFileName, GenerateStreamFromString(JsonSerializer.Serialize(info)));
            // zip.AddEntry(Constants.TagsFileName, GenerateStreamFromString("pseudo data"));
            //
            // var path = Path.GetTempPath() + Guid.NewGuid().ToString() + Constants.SatchelExtension;
            // zip.SaveTo(path, CompressionType.None);
            //
            // //Because satchel does not contain a codexInfo file, should work
            // var collection = await IOService.OpenSatchel(path);
            // Assert.IsNotNull(collection);
            // Directory.Delete(collection.FullDataPath, true);
            //
            // //Now add a codex file
            // zip.AddEntry(Constants.CodicesFileName, GenerateStreamFromString("Not important"));
            // zip.SaveTo(path, CompressionType.None);
            //
            // //Now that the codex file is added, should be null
            // collection = await IOService.OpenSatchel(path);
            // Assert.IsNull(collection);

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
