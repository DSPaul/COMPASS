using COMPASS.Infra.Collections;

namespace COMPASS.UnitTests.Infra.Collections;

[TestFixture]
public class CollectionExtensionsTests
{
    #region AddIfMissing

    [Test]
    public void AddIfMissing_AddsNewItem_ReturnsTrue()
    {
        List<int> numbers = [1, 2, 3];

        bool added = numbers.AddIfMissing(4);

        Assert.That(added, Is.True);
        Assert.That(numbers, Does.Contain(4));
    }

    [Test]
    public void AddIfMissing_ExistingItem_ReturnsFalse()
    {
        List<int> numbers = [1, 2, 3];

        bool added = numbers.AddIfMissing(2);

        Assert.That(added, Is.False);
        Assert.That(numbers, Has.Count.EqualTo(3));
    }

    [Test]
    public void AddIfMissing_NullItem_ThrowsArgumentNullException()
    {
        List<string> items = ["a"];

        Assert.Throws<ArgumentNullException>(() => items.AddIfMissing(null!));
    }

    #endregion

    #region Enumerable Extensions

    private class TreeNode : IHasChildren<TreeNode>
    {
        public string Name { get; }
        public RangeObservableCollection<TreeNode> Children { get; } = [];

        public TreeNode(string name, params TreeNode[] children)
        {
            Name = name;
            foreach (TreeNode child in children)
            {
                Children.Add(child);
            }
        }
    }

    [Test]
    public void Flatten_Dfs_ReturnsDepthFirstOrder()
    {
        var leaf1 = new TreeNode("Leaf1");
        var leaf2 = new TreeNode("Leaf2");
        var branch = new TreeNode("Branch", leaf1, leaf2);
        var root = new TreeNode("Root", branch);

        List<string> names = new[] { root }.Flatten("dfs").Select(n => n.Name).ToList();

        Assert.That(names, Is.EqualTo(["Root", "Branch", "Leaf1", "Leaf2"]));
    }

    [Test]
    public void Flatten_Bfs_ReturnsBreadthFirstOrder()
    {
        var leaf1 = new TreeNode("Leaf1");
        var leaf2 = new TreeNode("Leaf2");
        var branch = new TreeNode("Branch", leaf1, leaf2);
        var root = new TreeNode("Root", branch);

        List<string> names = new[] { root }.Flatten("bfs").Select(n => n.Name).ToList();

        Assert.That(names, Is.EqualTo(["Root", "Branch", "Leaf1", "Leaf2"]));
    }

    [Test]
    public void Without_RemovesElement()
    {
        int[] numbers = [1, 2, 3, 4];

        IEnumerable<int> result = numbers.Without(3);

        Assert.That(result, Is.EqualTo([1, 2, 4]));
    }

    [Test]
    public void Without_DoesNotModifyOriginal()
    {
        int[] numbers = [1, 2, 3, 4];

        IEnumerable<int> result = numbers.Without(3);

        Assert.That(numbers, Is.EqualTo([1, 2, 3, 4]));
    }

    [Test]
    public void RemoveNulls_FiltersOutNulls()
    {
        string?[] items = ["a", null, "b", null, "c"];

        IEnumerable<string> result = items.RemoveNulls();

        Assert.That(result, Is.EqualTo(["a", "b", "c"]));
    }

    [Test]
    public void SafeAny_NullCollection_ReturnsFalse()
    {
        IEnumerable<int>? nullList = null;

        Assert.That(nullList.SafeAny(), Is.False);
    }

    [Test]
    public void SafeAny_EmptyCollection_ReturnsFalse()
    {
        Assert.That(Array.Empty<int>().SafeAny(), Is.False);
    }

    [Test]
    public void SafeAny_NonEmptyCollection_ReturnsTrue()
    {
        Assert.That(new[] { 1 }.SafeAny(), Is.True);
    }

    [Test]
    public void HasCommonValue_AllSame_ReturnsTrue()
    {
        int[] items = [5, 5, 5];

        bool result = items.HasCommonValue(x => x, out int? value);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(5));
        });
    }

    [Test]
    public void HasCommonValue_Different_ReturnsFalse()
    {
        int[] items = [1, 2, 3];

        bool result = items.HasCommonValue(x => x, out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void HasCommonValue_Empty_ReturnsFalse()
    {
        int[] items = [];

        bool result = items.HasCommonValue(x => x, out _);

        Assert.That(result, Is.False);
    }

    #endregion
}
