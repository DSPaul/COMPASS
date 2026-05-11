using COMPASS.Infra.Models.Interfaces;
using InfraUtils = COMPASS.Infra.Tools.Utils;

namespace COMPASS.UnitTests.Infra.Tools;

[TestFixture]
public class UtilsTests
{
    private class IdItem : IHasId
    {
        public int Id { get; set; }
        public IdItem(int id) => Id = id;
    }

    [Test]
    public void GetAvailableId_EmptyCollection_ReturnsZero()
    {
        int id = InfraUtils.GetAvailableId(Array.Empty<IdItem>());

        Assert.That(id, Is.EqualTo(0));
    }

    [Test]
    public void GetAvailableId_ConsecutiveIds_ReturnsNext()
    {
        IdItem[] items = [new(0), new(1), new(2)];

        int id = InfraUtils.GetAvailableId(items);

        Assert.That(id, Is.EqualTo(3));
    }

    [Test]
    public void GetAvailableId_GapInIds_ReturnsFirstGap()
    {
        IdItem[] items = [new(0), new(2), new(3)];

        int id = InfraUtils.GetAvailableId(items);

        Assert.That(id, Is.EqualTo(1));
    }

    [Test]
    public void Retry_SucceedsOnFirstAttempt()
    {
        int callCount = 0;

        InfraUtils.Retry(3, () => callCount++, retryDelayMs: 0);

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Retry_SucceedsAfterFailures()
    {
        int callCount = 0;

        InfraUtils.Retry(3, () =>
        {
            callCount++;
            if (callCount < 3) throw new InvalidOperationException();
        }, retryDelayMs: 0);

        Assert.That(callCount, Is.EqualTo(3));
    }

    [Test]
    public void Retry_ExhaustsAttempts_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            InfraUtils.Retry(2, () => throw new InvalidOperationException(), retryDelayMs: 0));
    }

    [Test]
    public void Retry_CallsOnFailedAttempt()
    {
        int failureCount = 0;

        InfraUtils.Retry(3, () =>
        {
            if (failureCount < 2) throw new InvalidOperationException();
        }, retryDelayMs: 0, onFailedAttempt: _ => failureCount++);

        Assert.That(failureCount, Is.EqualTo(2));
    }

    [Test]
    public void Retry_GenericException_OnlyCatchesSpecifiedType()
    {
        Assert.Throws<ArgumentException>(() =>
            InfraUtils.Retry<InvalidOperationException>(3, () => throw new ArgumentException(), retryDelayMs: 0));
    }
}
