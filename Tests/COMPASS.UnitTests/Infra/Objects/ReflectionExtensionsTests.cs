using COMPASS.Infra.Objects;

namespace COMPASS.UnitTests.Infra.Objects;

[TestFixture]
public class ReflectionExtensionsTests
{
    #region Reflection Extensions

    private class TestObj
    {
        public string Name { get; set; } = "Test";
        public int Value { get; set; } = 42;
        public InnerObj Inner { get; set; } = new();
    }

    private class InnerObj
    {
        public string Deep { get; set; } = "DeepValue";
    }

    [Test]
    public void GetPropertyValue_ReturnsCorrectValue()
    {
        var obj = new TestObj();

        object? result = obj.GetPropertyValue("Name");

        Assert.That(result, Is.EqualTo("Test"));
    }

    [Test]
    public void GetPropertyValue_MissingProperty_Throws()
    {
        var obj = new TestObj();

        Assert.Throws<MissingFieldException>(() => obj.GetPropertyValue("NonExistent"));
    }

    [Test]
    public void GetDeepPropertyValue_NavigatesNestedProperties()
    {
        var obj = new TestObj();

        object? result = obj.GetDeepPropertyValue("Inner.Deep");

        Assert.That(result, Is.EqualTo("DeepValue"));
    }

    [Test]
    public void GetDeepPropertyValue_EmptyPath_ReturnsSelf()
    {
        var obj = new TestObj();

        object? result = obj.GetDeepPropertyValue("");

        Assert.That(result, Is.SameAs(obj));
    }

    [Test]
    public void SetProperty_SetsValue()
    {
        var obj = new TestObj();

        obj.SetProperty("Name", "NewName");

        Assert.That(obj.Name, Is.EqualTo("NewName"));
    }

    private class WithObsolete
    {
        public string Current { get; set; } = "";

        [Obsolete("Use Current instead")]
        public string Legacy { get; set; } = "";
    }

    [Test]
    public void GetObsoleteProperties_FindsObsoleteOnes()
    {
        List<string> obsolete = typeof(WithObsolete).GetObsoleteProperties();

        Assert.That(obsolete, Does.Contain("Legacy"));
        Assert.That(obsolete, Does.Not.Contain("Current"));
    }

    #endregion
}
