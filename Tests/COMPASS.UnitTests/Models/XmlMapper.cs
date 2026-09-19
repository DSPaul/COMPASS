using System.Text.Json;
using System.Xml.Serialization;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Tests.Common.DataGenerators;

namespace COMPASS.UnitTests.Models
{
    [TestFixture]
    public class XmlMapper
    {
        [Test]
        public void MapCodex()
        {
            // -2: Sources are 3 props in dto, but only 1 in model
            // +1: Collection is a runtime back-reference on the model, passed as a param in ToModel
            int expectedDiff = -1;

            AssertAllPropMapped(typeof(Codex), typeof(CodexDto), expectedDiff);

            var collection = new CodexCollection("__testCollection");

            Codex codex = RandomGenerator.GetRandomCodex(collection);

            CodexDto dto = codex.ToDto();
            Codex reconstruction = dto.ToModel(collection);

            Assert.That(
                JsonSerializer.Serialize(reconstruction),
                Is.EqualTo(JsonSerializer.Serialize(codex)));
        }

        [Test]
        public void MapCodexProperty()
        {
            AssertAllPropMapped(typeof(CodexProperty), typeof(CodexPropertyDto));

            CodexProperty codexProp = CodexProperty.GetInstance(nameof(Codex.Title))!;

            CodexPropertyDto dto = codexProp.ToDto();
            CodexProperty reconstruction = dto.ToModel()!;

            Assert.That(
                JsonSerializer.Serialize(reconstruction),
                Is.EqualTo(JsonSerializer.Serialize(codexProp)));
        }

        [Test]
        public void MapPreferences()
        {
            AssertAllPropMapped(typeof(Preferences), typeof(PreferencesDto));

            Preferences prefs = RandomGenerator.GetRandomPreferences();

            PreferencesDto dto = prefs.ToDto();
            Preferences reconstruction = dto.ToModel()!;

            //Funcs cannot be serialized
            Assert.That(prefs.OpenCodexPriority.SequenceEqual(reconstruction.OpenCodexPriority), Is.True);
            prefs.OpenCodexPriority = [];
            reconstruction.OpenCodexPriority = [];

            Assert.That(
                JsonSerializer.Serialize(reconstruction),
                Is.EqualTo(JsonSerializer.Serialize(prefs)));
        }

        [Test]
        public void SourcePriority_PreRenameXmlFormat_Deserializes()
        {
            // Files written before the MetaDataSourceType -> MetadataSourceType rename
            // contain <MetaDataSourceType> list items; member names never changed.
            const string legacyXml = """
                <CodexProperty>
                  <Name>Title</Name>
                  <SourcePriority>
                    <MetaDataSourceType>PDF</MetaDataSourceType>
                    <MetaDataSourceType>ISBN</MetaDataSourceType>
                  </SourcePriority>
                  <OverwriteMode>IfEmpty</OverwriteMode>
                </CodexProperty>
                """;

            XmlSerializer serializer = new(typeof(CodexPropertyDto));
            using StringReader reader = new(legacyXml);
            CodexPropertyDto? dto = serializer.Deserialize(reader) as CodexPropertyDto;

            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.SourcePriority, Is.EqualTo(new[] { MetadataSourceType.PDF, MetadataSourceType.ISBN }));
        }

        [Test]
        public void SourcePriority_SerializesWithLegacyElementName()
        {
            CodexPropertyDto dto = new()
            {
                Name = "Title",
                SourcePriority = [MetadataSourceType.PDF]
            };

            XmlSerializer serializer = new(typeof(CodexPropertyDto));
            using StringWriter writer = new();
            serializer.Serialize(writer, dto);

            Assert.That(writer.ToString(), Does.Contain("<MetaDataSourceType>PDF</MetaDataSourceType>"));
        }

        private static void AssertAllPropMapped(Type modelType, Type dtoType, int expectedDiff = 0)        {
            var modelPropsCount = modelType
                .GetProperties()
                .Count(prop => prop.CanWrite && !prop.IsDefined(typeof(ObsoleteAttribute), false));

            var dtoPropCount = dtoType
                .GetProperties()
                .Count(prop => prop.CanWrite && !prop.IsDefined(typeof(ObsoleteAttribute), false));

            Assert.That(modelPropsCount, Is.EqualTo(dtoPropCount + expectedDiff));
        }
    }
}