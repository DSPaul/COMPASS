using System.Globalization;
using COMPASS.Infra.Avalonia.Converters;

namespace COMPASS.UnitTests.Infra.Avalonia.Converters;

[TestFixture]
public class CollectionUnionConverterTests
{
    private CollectionUnionConverter _converter = null!;

    [SetUp]
    public void SetUp()
    {
        _converter = new CollectionUnionConverter();
    }

    [Test]
    public void Convert_TwoCollections_ReturnsUnion()
    {
        List<object?> values = [new List<int> { 1, 2, 3 }, new List<int> { 3, 4, 5 }];

        object? result = _converter.Convert(values, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.EquivalentTo([1, 2, 3, 4, 5]));
    }

    [Test]
    public void Convert_EmptyCollections_ReturnsEmpty()
    {
        List<object?> values = [new List<int>(), new List<int>()];

        object? result = _converter.Convert(values, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Convert_NullValuesSkipped_ReturnsNonNullItems()
    {
        List<object?> values = [null, new List<int> { 1, 2 }];

        object? result = _converter.Convert(values, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.EquivalentTo([1, 2]));
    }

    [Test]
    public void Convert_DuplicatesRemoved()
    {
        List<object?> values = [new List<string> { "a", "b" }, new List<string> { "b", "c" }];

        object? result = _converter.Convert(values, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.EquivalentTo(["a", "b", "c"]));
    }
}
