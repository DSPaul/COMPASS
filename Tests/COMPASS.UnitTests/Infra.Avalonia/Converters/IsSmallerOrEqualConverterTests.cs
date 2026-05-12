using System.Globalization;
using COMPASS.Infra.Avalonia.Converters;

namespace COMPASS.UnitTests.Infra.Avalonia.Converters;

[TestFixture]
public class IsSmallerOrEqualConverterTests
{
    private IsSmallerOrEqualConverter _converter = null!;

    [SetUp]
    public void SetUp()
    {
        _converter = new IsSmallerOrEqualConverter();
    }

    [Test]
    public void Convert_FirstSmallerThanSecond_ReturnsTrue()
    {
        List<object?> values = [3, 5];

        object? result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.True);
    }

    [Test]
    public void Convert_FirstEqualToSecond_ReturnsTrue()
    {
        List<object?> values = [5, 5];

        object? result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.True);
    }

    [Test]
    public void Convert_FirstGreaterThanSecond_ReturnsFalse()
    {
        List<object?> values = [7, 5];

        object? result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Convert_WrongNumberOfValues_ThrowsArgumentException()
    {
        List<object?> values = [5];

        Assert.That(() => _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture),
            Throws.TypeOf<ArgumentException>());
    }
}
