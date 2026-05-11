using System.Globalization;
using COMPASS.Infra.Avalonia.Converters;

namespace COMPASS.UnitTests.Infra.Avalonia.Converters;

[TestFixture]
public class AreEqualConverterTests
{
    private AreEqualConverter _converter = null!;

    [SetUp]
    public void SetUp()
    {
        _converter = new AreEqualConverter();
    }

    [Test]
    public void Convert_EqualValues_ReturnsTrue()
    {
        List<object?> values = [42, 42];

        object? result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.True);
    }

    [Test]
    public void Convert_BothNull_ReturnsFalse()
    {
        List<object?> values = [null, null];

        object? result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Convert_DifferentValues_ReturnsFalse()
    {
        List<object?> values = [42, 99];

        object? result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Convert_EqualStrings_ReturnsTrue()
    {
        List<object?> values = ["hello", "hello"];

        object? result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.True);
    }

    [Test]
    public void Convert_NullValue_ReturnsFalse()
    {
        List<object?> values = [null, 42];

        object? result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Convert_LessThanTwoValues_ReturnsFalse()
    {
        List<object?> values = [42];

        object? result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Convert_Inverted_EqualValues_ReturnsFalse()
    {
        _converter.Invert = true;
        List<object?> values = [42, 42];

        object? result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Convert_Inverted_DifferentValues_ReturnsTrue()
    {
        _converter.Invert = true;
        List<object?> values = [42, 99];

        object? result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.True);
    }
}
