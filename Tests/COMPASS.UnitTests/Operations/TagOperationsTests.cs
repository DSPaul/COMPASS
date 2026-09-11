using Avalonia.Media;
using COMPASS.Common.Models;
using COMPASS.Common.Operations;
using COMPASS.Infra.Models;

namespace COMPASS.UnitTests.Operations
{
    [TestFixture]
    public class TagOperationsTests
    {
        [Test]
        public void DeepClone_TagTree_CreatesIndependentCopiesWithSameStructure()
        {
            // Arrange
            var grandchild = new Tag { Name = "Grandchild", LinkedGlobs = new(["*.pdf"]) };
            var child = new Tag
            {
                Name = "Child",
                InternalBackgroundColor = Colors.Red,
                Children = new RangeObservableCollection<Tag> { grandchild }
            };
            var root = new Tag
            {
                Name = "Root",
                IsGroup = true,
                Children = new RangeObservableCollection<Tag> { child }
            };

            // Act
            Tag clone = TagOperations.DeepClone(root, out var map);

            // Assert - structure preserved
            Assert.That(clone.Name, Is.EqualTo("Root"));
            Assert.That(clone.IsGroup, Is.True);
            Assert.That(clone.Parent, Is.Null);

            Assert.That(clone.Children.Count, Is.EqualTo(1));
            Tag clonedChild = clone.Children[0];
            Assert.That(clonedChild.Name, Is.EqualTo("Child"));
            Assert.That(clonedChild.InternalBackgroundColor, Is.EqualTo(Colors.Red));
            Assert.That(clonedChild.Parent, Is.SameAs(clone));

            Assert.That(clonedChild.Children.Count, Is.EqualTo(1));
            Tag clonedGrandchild = clonedChild.Children[0];
            Assert.That(clonedGrandchild.Name, Is.EqualTo("Grandchild"));
            Assert.That(clonedGrandchild.LinkedGlobs.ToList(), Is.EqualTo(new List<string> { "*.pdf" }));
            Assert.That(clonedGrandchild.Parent, Is.SameAs(clonedChild));

            // Assert - fully independent instances
            Assert.That(clone, Is.Not.SameAs(root));
            Assert.That(clonedChild, Is.Not.SameAs(child));
            Assert.That(clonedGrandchild, Is.Not.SameAs(grandchild));
            Assert.That(clonedGrandchild.LinkedGlobs, Is.Not.SameAs(grandchild.LinkedGlobs));

            // Assert - map covers every tag
            Assert.That(map.Count, Is.EqualTo(3));
            Assert.That(map[root], Is.SameAs(clone));
            Assert.That(map[child], Is.SameAs(clonedChild));
            Assert.That(map[grandchild], Is.SameAs(clonedGrandchild));

            // Assert - source untouched
            Assert.That(child.Parent, Is.SameAs(root));
            Assert.That(grandchild.Parent, Is.SameAs(child));
            Assert.That(root.Children[0], Is.SameAs(child));
        }

        [Test]
        public void DeepCloneRoots_MultipleRoots_MapsEveryTag()
        {
            // Arrange
            var child = new Tag { Name = "Child" };
            var firstRoot = new Tag
            {
                Name = "First",
                Children = new RangeObservableCollection<Tag> { child }
            };
            var secondRoot = new Tag { Name = "Second" };

            // Act
            List<Tag> clones = TagOperations.DeepCloneTags([firstRoot, secondRoot], out var map);

            // Assert
            Assert.That(clones.Count, Is.EqualTo(2));
            Assert.That(map.Count, Is.EqualTo(3));

            Assert.That(clones[0].Children[0].Parent, Is.SameAs(clones[0]));
            Assert.That(clones[1].Parent, Is.Null);

            // Mutating a clone must not affect the source tree
            clones[0].Name = "Changed";
            clones[0].Children[0].Name = "ChangedChild";

            Assert.That(firstRoot.Name, Is.EqualTo("First"));
            Assert.That(child.Name, Is.EqualTo("Child"));
            Assert.That(secondRoot.Name, Is.EqualTo("Second"));
        }
    }
}
