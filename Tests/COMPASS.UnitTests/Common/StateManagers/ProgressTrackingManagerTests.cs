using COMPASS.Common.Services.StateManagers;
using COMPASS.Infra.Models.Measuring;
using COMPASS.Infra.Models.Progress;

namespace COMPASS.UnitTests.Common.StateManagers;

[TestFixture]
public class ProgressTrackingManagerTests
{
    private ProgressTrackingManager _manager = null!;

    [SetUp]
    public void SetUp() => _manager = new ProgressTrackingManager();

    [TearDown]
    public void TearDown() => _manager.Dispose();

    [Test]
    public void Track_Tracker_AppearsInSnapshot()
    {
        ProgressTracker tracker = new(Quantities.Items());

        using TrackedOperation operation = _manager.Track(tracker, "Doing stuff");

        Assert.That(_manager.GetSnapshot(), Has.Count.EqualTo(1));
        Assert.That(_manager.GetSnapshot()[0], Is.SameAs(operation));
        Assert.That(operation.Title, Is.EqualTo("Doing stuff"));
        Assert.That(operation.Tracker, Is.SameAs(tracker));
    }

    [Test]
    public async Task RunAsync_Work_UntracksAfterCompletion()
    {
        ProgressTracker tracker = new(Quantities.Items());

        await _manager.RunAsync(tracker, "Doing stuff", (reportedTracker, _) =>
        {
            Assert.That(reportedTracker, Is.SameAs(tracker));
            return Task.CompletedTask;
        });

        Assert.That(_manager.GetSnapshot(), Is.Empty);
    }

    [Test]
    public async Task RunAsync_FaultedWork_UntracksAndPropagates()
    {
        var workException = new InvalidOperationException("boom");
        ProgressTracker tracker = new(Quantities.Items());

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _manager.RunAsync(tracker, "Doing stuff", (_, _) => Task.FromException(workException)));

        Assert.That(_manager.GetSnapshot(), Is.Empty);
    }

    [Test]
    public void CancelAll_TrackedOperation_ObservesCancellation()
    {
        ProgressTracker tracker = new(Quantities.Items());
        using TrackedOperation operation = _manager.Track(tracker, "Doing stuff");

        Assert.That(operation.CancellationToken.IsCancellationRequested, Is.False);

        _manager.CancelAll();

        Assert.That(operation.CancellationToken.IsCancellationRequested, Is.True);
    }

    [Test]
    public void Cancel_SingleOperation_LeavesOthersRunning()
    {
        using TrackedOperation firstOperation = _manager.Track(new(Quantities.Items()), "First");
        using TrackedOperation secondOperation = _manager.Track(new(Quantities.Items()), "Second");

        firstOperation.Cancel();

        Assert.That(firstOperation.CancellationToken.IsCancellationRequested, Is.True);
        Assert.That(secondOperation.CancellationToken.IsCancellationRequested, Is.False);
        Assert.That(_manager.GetSnapshot(), Has.Count.EqualTo(2));
    }
}
