using COMPASS.Common.Models.Enums;
using COMPASS.Infra.Text;
using System.ComponentModel.DataAnnotations;

namespace COMPASS.UnitTests.Infra.Text;

[TestFixture]
public class EnumDisplayExtensionsTests
{
    [Flags]
    private enum SampleOptions
    {
        [Display(Name = "First option")] First = 1,
        Second = 2,
        [Display(Name = "Third option")] Third = 4,
    }

    [Test]
    public void GetDisplayName_MemberWithDisplayAttribute_ReturnsAttributeName()
    {
        string displayName = SampleOptions.First.GetDisplayName();

        Assert.That(displayName, Is.EqualTo("First option"));
    }

    [Test]
    public void GetDisplayName_MemberWithoutDisplayAttribute_FallsBackToMemberName()
    {
        string displayName = SampleOptions.Second.GetDisplayName();

        Assert.That(displayName, Is.EqualTo("Second"));
    }

    [Test]
    public void GetDisplayName_CombinedFlags_FallsBackToToString()
    {
        SampleOptions combinedOptions = SampleOptions.First | SampleOptions.Third;

        string displayName = combinedOptions.GetDisplayName();

        Assert.That(displayName, Is.EqualTo(combinedOptions.ToString()));
    }

    [Test]
    public void GetDisplayName_UndefinedValue_FallsBackToToString()
    {
        SampleOptions undefinedOption = (SampleOptions)64;

        string displayName = undefinedOption.GetDisplayName();

        Assert.That(displayName, Is.EqualTo("64"));
    }

    [Test]
    public void GetDisplayName_SameValueTwice_ReturnsSameName()
    {
        string firstLookup = SampleOptions.Third.GetDisplayName();

        string secondLookup = SampleOptions.Third.GetDisplayName();

        Assert.That(secondLookup, Is.EqualTo(firstLookup));
    }

    [Test]
    public void MetadataSourceType_AllMembers_HaveDisplayName()
    {
        MetadataSourceType[] sourceTypes = Enum.GetValues<MetadataSourceType>();

        IEnumerable<MetadataSourceType> sourceTypesWithoutDisplayName = sourceTypes
            .Where(sourceType => typeof(MetadataSourceType)
                .GetField(sourceType.ToString())!
                .GetCustomAttributes(typeof(DisplayAttribute), false)
                .Length == 0);

        Assert.That(sourceTypesWithoutDisplayName, Is.Empty);
    }
}
