using System.Text.Json;
using COMPASS.Common.Models;
using COMPASS.Common.Services;

namespace COMPASS.UnitTests
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
                Assert.That(newSatchelInfo!.CreationVersion, Is.EqualTo(ApplicationService.Version));
                Assert.That(newSatchelInfo!.CreationDate.Date, Is.EqualTo(DateTime.Now.Date));
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
            Assert.That(satchelInfo!.CreationVersion, Is.EqualTo("1.200.0"));
        }

        [Test]
        public void DeserializeIncompleteSatchelInfo()
        {
            //When incomplete, should fall back to defaults

            string json = @"{
                    ""CreationVersion"":""1.200.0"",
                    ""CreationDate"":""2024-02-22T20:08:07.1844264+01:00""}";

            var satchelInfo = JsonSerializer.Deserialize<SatchelInfo>(json);
            Assert.That(satchelInfo, Is.Not.Null);
            Assert.That(satchelInfo!.MinTagsVersion, Is.EqualTo(new SatchelInfo().MinTagsVersion));
        }
    }
}
