using System.Collections.Specialized;
using COMPASS.Infra.Models;

namespace COMPASS.UnitTests.Infra.Models;

[TestFixture]
public class RangeObservableCollectionTests
{
    [Test]
    public void AddRange_AddsAllItems()
    {
        var collection = new RangeObservableCollection<int>([0]);

        collection.AddRange([1, 2, 3]);

        Assert.That(collection, Is.EqualTo([0, 1, 2, 3]));
    }

    [Test]
    public void AddRange_EmptyCollection_NoChange()
    {
        var collection = new RangeObservableCollection<int>([10]);

        collection.AddRange([]);

        Assert.That(collection, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddRange_FiresSingleCollectionChangedEvent()
    {
        var collection = new RangeObservableCollection<int>();
        int eventCount = 0;
        collection.CollectionChanged += (_, _) => eventCount++;

        collection.AddRange([1, 2, 3]);

        Assert.That(eventCount, Is.EqualTo(1));
    }

    [Test]
    public void InsertRange_InsertsAtCorrectPosition()
    {
        var collection = new RangeObservableCollection<int>([1, 4]);

        collection.InsertRange(1, [2, 3]);

        Assert.That(collection, Is.EqualTo([1, 2, 3, 4]));
    }

    [Test]
    public void InsertRange_NullCollection_Throws()
    {
        var collection = new RangeObservableCollection<int>();

        Assert.Throws<ArgumentNullException>(() => collection.InsertRange(0, null!));
    }

    [Test]
    public void InsertRange_NegativeIndex_Throws()
    {
        var collection = new RangeObservableCollection<int>();

        Assert.Throws<ArgumentOutOfRangeException>(() => collection.InsertRange(-1, [1]));
    }

    [Test]
    public void RemoveRange_RemovesSpecifiedItems()
    {
        var collection = new RangeObservableCollection<int>([1, 2, 3, 4, 5]);

        collection.RemoveRange([2, 4]);

        Assert.That(collection, Is.EqualTo([1, 3, 5]));
    }

    [Test]
    public void RemoveRange_ItemNotInCollection_SkipsIt()
    {
        var collection = new RangeObservableCollection<int>([1, 2, 3]);

        collection.RemoveRange([4, 5]);

        Assert.That(collection, Is.EqualTo([1, 2, 3]));
    }

    [Test]
    public void RemoveRange_NullCollection_Throws()
    {
        var collection = new RangeObservableCollection<int>();

        Assert.Throws<ArgumentNullException>(() => collection.RemoveRange(null!));
    }

    [Test]
    public void Constructor_WithCollection_CopiesElements()
    {
        int[] source = [10, 20, 30];

        var collection = new RangeObservableCollection<int>(source);

        Assert.That(collection, Is.EqualTo(source));
    }

    [Test]
    public void Constructor_WithList_CopiesElements()
    {
        List<int> source = [10, 20, 30];

        var collection = new RangeObservableCollection<int>(source);

        Assert.That(collection, Is.EqualTo(source));
    }

    [Test]
    public void Sort_Ascending_SortsCorrectly()
    {
        var collection = new RangeObservableCollection<int>([3, 1, 2]);

        collection.Sort(x => x);

        Assert.That(collection, Is.EqualTo([1, 2, 3]));
    }

    [Test]
    public void Sort_Descending_SortsCorrectly()
    {
        var collection = new RangeObservableCollection<int>([1, 3, 2]);

        collection.Sort(x => x, System.ComponentModel.ListSortDirection.Descending);

        Assert.That(collection, Is.EqualTo([3, 2, 1]));
    }
}
