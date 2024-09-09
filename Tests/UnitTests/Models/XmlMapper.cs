using COMPASS.Models;
using COMPASS.Models.CodexProperties;
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
            int expectedDiff = 0;

            AssertAllPropMapped(typeof(CodexProperty), typeof(CodexPropertyDto), expectedDiff);

            CodexProperty codexProp = CodexProperty.GetInstance(nameof(Codex.Title))!;

            CodexPropertyDto dto = codexProp.ToDto();
            CodexProperty reconstrution = dto.ToModel()!;

            Assert.AreEqual(
                JsonSerializer.Serialize(reconstrution),
                JsonSerializer.Serialize(codexProp));
        }

        private void AssertAllPropMapped(Type modelType, Type dtoType, int expectedDiff)
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
