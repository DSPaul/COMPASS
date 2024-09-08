using COMPASS.Models;
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
            AssertAllPropMapped(typeof(Codex), typeof(CodexDto));

            Codex codex = RandomGenerator.GetRandomCodex();

            CodexDto dto = codex.ToDto();
            Codex reconstrution = dto.ToModel(codex.Tags);

            Assert.AreEqual(
                JsonSerializer.Serialize(reconstrution),
                JsonSerializer.Serialize(codex));
        }

        private void AssertAllPropMapped(Type modelType, Type dtoType)
        {
            var modelPropsCount = modelType
               .GetProperties()
               .Where(prop => prop.CanWrite)
               .Count();

            var dtoPropCount = dtoType
                .GetProperties()
                .Where(prop => prop.CanWrite)
                .Count();

            Assert.AreEqual(modelPropsCount, dtoPropCount);
        }
    }
}
