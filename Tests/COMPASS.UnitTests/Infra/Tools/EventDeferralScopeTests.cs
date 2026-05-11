using COMPASS.Infra.Tools;

namespace COMPASS.UnitTests.Infra.Tools;

[TestFixture]
public class EventDeferralScopeTests
{
    [Test]
    public void Notify_OutsideScope_FiresImmediately()
    {
        int callCount = 0;
        var deferralScope = new EventDeferralScope(_ => callCount++);

        deferralScope.Notify(EventArgs.Empty);

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Notify_InsideScope_FiresOnScopeExit()
    {
        int callCount = 0;
        var deferralScope = new EventDeferralScope(_ => callCount++);

        using (deferralScope.BeginDeferral())
        {
            deferralScope.Notify(EventArgs.Empty);
            Assert.That(callCount, Is.EqualTo(0));
        }
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Notify_MultipleDistinctArgs_FiresEachOnce()
    {
        var firedArgs = new List<EventArgs>();
        var deferralScope = new EventDeferralScope<EventArgs>(arg => firedArgs.Add(arg));

        var args1 = new EventArgs();
        var args2 = new EventArgs();

        using (deferralScope.BeginDeferral())
        {
            deferralScope.Notify(args1);
            deferralScope.Notify(args2);
        }

        Assert.That(firedArgs, Is.EqualTo([args1, args2]));
    }

    [Test]
    public void Notify_DuplicateArgs_FiresOnlyOnce()
    {
        int callCount = 0;
        var deferralScope = new EventDeferralScope<EventArgs>(_ => callCount++);

        var sameArgs = new EventArgs();

        using (deferralScope.BeginDeferral())
        {
            deferralScope.Notify(sameArgs);
            deferralScope.Notify(sameArgs);
            deferralScope.Notify(sameArgs);
        }

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Notify_NoNotificationsInsideScope_NothingFiredOnExit()
    {
        int callCount = 0;
        var deferralScope = new EventDeferralScope(_ => callCount++);

        using (deferralScope.BeginDeferral())
        {
            // no notifications
        }

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void Notify_AfterScopeExits_FiresImmediatelyAgain()
    {
        int callCount = 0;
        var deferralScope = new EventDeferralScope(_ => callCount++);

        using (deferralScope.BeginDeferral())
        {
            deferralScope.Notify(EventArgs.Empty);
        }

        deferralScope.Notify(EventArgs.Empty);

        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public void NestedScopes_OnlyOutermostScopeFiresEvents()
    {
        var firedArgs = new List<EventArgs>();
        var deferralScope = new EventDeferralScope<EventArgs>(arg => firedArgs.Add(arg));

        var args1 = new EventArgs();
        var args2 = new EventArgs();

        using (deferralScope.BeginDeferral())
        {
            deferralScope.Notify(args1);

            using (deferralScope.BeginDeferral()) // inner scope — should be a no-op
            {
                deferralScope.Notify(args2);
                Assert.That(firedArgs, Is.Empty);
            }

            // inner scope disposed, but outer is still active — nothing fired yet
            Assert.That(firedArgs, Is.Empty);
        }

        Assert.That(firedArgs, Is.EqualTo([args1, args2]));
    }

    [Test]
    public void DeferralScope_EventArgs_ClearedAfterScopeExits()
    {
        int callCount = 0;
        var deferralScope = new EventDeferralScope(_ => callCount++);

        using (deferralScope.BeginDeferral())
        {
            deferralScope.Notify(EventArgs.Empty);
        }

        // Second scope should not re-fire events from the first
        using (deferralScope.BeginDeferral())
        {
            // no notifications
        }

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void NonGeneric_Notify_WithoutArgs_FiresAction()
    {
        int callCount = 0;
        var deferralScope = new EventDeferralScope(_ => callCount++);

        deferralScope.Notify();

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void NonGeneric_Notify_InsideScope_DefersAndFires()
    {
        int callCount = 0;
        var deferralScope = new EventDeferralScope(_ => callCount++);

        using (deferralScope.BeginDeferral())
        {
            deferralScope.Notify();
            Assert.That(callCount, Is.EqualTo(0));
        }

        Assert.That(callCount, Is.EqualTo(1));
    }
}
