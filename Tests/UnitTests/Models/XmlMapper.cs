using COMPASS.Models;
using COMPASS.Models.CodexProperties;
using COMPASS.Models.Preferences;
using COMPASS.Models.XmlDtos;
using System.Text.Json;
using Tests.DataGenerators;

namespace Tests.UnitTests.Models
{
    [TestClass]
    public class XmlMapper
    {
        [TestMethod]
        public void MapCodex()
        {
            // Sources are 3 props in dto, but only 1 in model
            int expectedDiff = -2;

            AssertAllPropMapped(typeof(Codex), typeof(CodexDto), expectedDiff);

            Codex codex = RandomGenerator.GetRandomCodex();

            CodexDto dto = codex.ToDto();
            Codex reconstrution = dto.ToModel(codex.Tags);

            Assert.AreEqual(
                JsonSerializer.Serialize(reconstrution),
                JsonSerializer.Serialize(codex));
        }

        [TestMethod]
        public void MapCodexProperty()
        {
            AssertAllPropMapped(typeof(CodexProperty), typeof(CodexPropertyDto));

            CodexProperty codexProp = CodexProperty.GetInstance(nameof(Codex.Title))!;

            CodexPropertyDto dto = codexProp.ToDto();
            CodexProperty reconstrution = dto.ToModel()!;

            Assert.AreEqual(
                JsonSerializer.Serialize(reconstrution),
                JsonSerializer.Serialize(codexProp));
        }

        [TestMethod]
        public void MapPreferences()
        {
            AssertAllPropMapped(typeof(Preferences), typeof(PreferencesDto));

            Preferences prefs = RandomGenerator.GetRandomPreferences();

            PreferencesDto dto = prefs.ToDto();
            Preferences reconstrution = dto.ToModel()!;

            //Funcs cannot be serialized
            Assert.IsTrue(prefs.OpenCodexPriority.SequenceEqual(reconstrution.OpenCodexPriority));
            prefs.OpenCodexPriority = new System.Collections.ObjectModel.ObservableCollection<PreferableFunction<Codex>>();
            reconstrution.OpenCodexPriority = new System.Collections.ObjectModel.ObservableCollection<PreferableFunction<Codex>>();

            Assert.AreEqual(
                JsonSerializer.Serialize(reconstrution),
                JsonSerializer.Serialize(prefs));
        }

        private void AssertAllPropMapped(Type modelType, Type dtoType, int expectedDiff = 0)
        {
            var modelPropsCount = modelType
               .GetProperties()
               .Where(prop => prop.CanWrite && !prop.IsDefined(typeof(ObsoleteAttribute), false))
               .Count();

            var dtoPropCount = dtoType
                .GetProperties()
                .Where(prop => prop.CanWrite && !prop.IsDefined(typeof(ObsoleteAttribute), false))
                .Count();

            Assert.AreEqual(modelPropsCount, dtoPropCount + expectedDiff);
        }
    }
}
