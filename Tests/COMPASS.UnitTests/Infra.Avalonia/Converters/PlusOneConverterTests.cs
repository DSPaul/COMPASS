using System.Globalization;
using Avalonia.Data;
using COMPASS.Infra.Avalonia.Converters;

namespace COMPASS.UnitTests.Infra.Avalonia.Converters;

[TestFixture]
public class PlusOneConverterTests
{
    private PlusOneConverter _converter = null!;

    [SetUp]
    public void SetUp()
    {
        _converter = new PlusOneConverter();
    }

    [Test]
    public void Convert_IntegerValue_ReturnsValuePlusOne()
    {
        object result = _converter.Convert(5, typeof(int), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void Convert_Zero_ReturnsOne()
    {
        object result = _converter.Convert(0, typeof(int), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Convert_NonInteger_ReturnsBindingNotification()
    {
        object result = _converter.Convert("not an int", typeof(int), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.InstanceOf<BindingNotification>());
    }

    [Test]
    public void ConvertBack_IntegerValue_ReturnsValueMinusOne()
    {
        object result = _converter.ConvertBack(5, typeof(int), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void ConvertBack_NonInteger_ReturnsBindingNotification()
    {
        object result = _converter.ConvertBack("not an int", typeof(int), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.InstanceOf<BindingNotification>());
    }
}
