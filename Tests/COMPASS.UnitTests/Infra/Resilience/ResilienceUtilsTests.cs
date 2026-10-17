using COMPASS.Infra.Resilience;

namespace COMPASS.UnitTests.Infra.Resilience;

[TestFixture]
public class ResilienceUtilsTests
{
    [Test]
    public void Retry_SucceedsOnFirstAttempt()
    {
        int callCount = 0;

        ResilienceUtils.Retry(3, () => callCount++, retryDelayMs: 0);

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Retry_SucceedsAfterFailures()
    {
        int callCount = 0;

        ResilienceUtils.Retry(3, () =>
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
            ResilienceUtils.Retry(2, () => throw new InvalidOperationException(), retryDelayMs: 0));
    }

    [Test]
    public void Retry_CallsOnFailedAttempt()
    {
        int failureCount = 0;

        ResilienceUtils.Retry(3, () =>
        {
            if (failureCount < 2) throw new InvalidOperationException();
        }, retryDelayMs: 0, onFailedAttempt: _ => failureCount++);

        Assert.That(failureCount, Is.EqualTo(2));
    }

    [Test]
    public void Retry_GenericException_OnlyCatchesSpecifiedType()
    {
        Assert.Throws<ArgumentException>(() =>
            ResilienceUtils.Retry<InvalidOperationException>(3, () => throw new ArgumentException(), retryDelayMs: 0));
    }
}
