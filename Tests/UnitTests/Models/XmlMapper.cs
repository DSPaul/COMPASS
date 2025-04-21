using System.Text.Json;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Models.XmlDtos;
using Tests.DataGenerators;

namespace Tests.UnitTests.Models
{
    [TestFixture]
    public class XmlMapper
    {
        [Test]
        public void MapCodex()
        {
            // Sources are 3 props in dto, but only 1 in model
            int expectedDiff = -2;

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
            CodexProperty reconstrution = dto.ToModel()!;

            Assert.That(
                JsonSerializer.Serialize(reconstrution),
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

        private static void AssertAllPropMapped(Type modelType, Type dtoType, int expectedDiff = 0)
        {
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