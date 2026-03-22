using NUnit.Framework;
using COMPASS.Common.Models;
using System;
using COMPASS.Common.Attributes;
using System.Reflection;

namespace COMPASS.UnitTests.Models
{
    [TestFixture]
    public class CodexTests
    {
        private static IEnumerable<TestCaseData> PersonalPropertyTestCases
        {
            get
            {
                yield return new TestCaseData(nameof(Codex.Rating), 5, 0);
                yield return new TestCaseData(nameof(Codex.PhysicallyOwned), true, false);
                yield return new TestCaseData(nameof(Codex.Favorite), true, false);
                yield return new TestCaseData(nameof(Codex.OpenedCount), 10, 0);
                yield return new TestCaseData(nameof(Codex.LastOpened), DateTime.MaxValue, default(DateTime));
            }
        }

        [TestCaseSource(nameof(PersonalPropertyTestCases))]
        public void ResetPersonalProperty_Resets_Property_To_ExpectedValue(string propertyName, object dirtyValue, object expectedValue)
        {
            var collection = new CodexCollection("Test");
            var codex = new Codex(collection);
            var propInfo = typeof(Codex).GetProperty(propertyName);

            Assert.That(propInfo, Is.Not.Null);
            
            propInfo.SetValue(codex, dirtyValue);
            
            var value = propInfo.GetValue(codex);
            Assert.That(value, Is.EqualTo(dirtyValue));
            
            codex.ResetPersonalProperty(propertyName);

            var actualValue = propInfo.GetValue(codex);
            Assert.That(actualValue, Is.EqualTo(expectedValue));
        }

        [Test]
        public void ResetPersonalProperty_Resets_DateAdded_ToNow()
        {
            var collection = new CodexCollection("Test");
            var codex = new Codex(collection);
            var dirtyValue = DateTime.Now.AddDays(-10);
            codex.DateAdded = dirtyValue;

            codex.ResetPersonalProperty(nameof(Codex.DateAdded));

            Assert.That(codex.DateAdded, Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(5)));
            Assert.That(codex.DateAdded, Is.Not.EqualTo(dirtyValue));
        }

        [TestCase("Title")]
        [TestCase("Description")]
        public void ResetPersonalProperty_DoesNotReset_NonPersonalProperties(string propertyName)
        {
             var collection = new CodexCollection("Test");
             var codex = new Codex(collection);
             codex.Title = "Original Title";
             codex.Description = "Original Description";

             codex.ResetPersonalProperty(propertyName);

             Assert.That(codex.Title, Is.EqualTo("Original Title"));
             Assert.That(codex.Description, Is.EqualTo("Original Description"));
        }
    }
}





