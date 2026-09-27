using COMPASS.Infra.Collections;

namespace COMPASS.UnitTests.Infra.Collections;

[TestFixture]
public class IdGeneratorTests
{
    private class IdItem : IHasId
    {
        public int Id { get; set; }
        public IdItem(int id) => Id = id;
    }

    [Test]
    public void GetAvailableId_EmptyCollection_ReturnsZero()
    {
        int id = IdGenerator.GetAvailableId(Array.Empty<IdItem>());

        Assert.That(id, Is.EqualTo(0));
    }

    [Test]
    public void GetAvailableId_ConsecutiveIds_ReturnsNext()
    {
        IdItem[] items = [new(0), new(1), new(2)];

        int id = IdGenerator.GetAvailableId(items);

        Assert.That(id, Is.EqualTo(3));
    }

    [Test]
    public void GetAvailableId_GapInIds_ReturnsFirstGap()
    {
        IdItem[] items = [new(0), new(2), new(3)];

        int id = IdGenerator.GetAvailableId(items);

        Assert.That(id, Is.EqualTo(1));
    }
}
