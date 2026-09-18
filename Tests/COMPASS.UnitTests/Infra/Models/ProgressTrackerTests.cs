using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Models.Measuring;
using COMPASS.Infra.Models.Progress;
using COMPASS.Infra.Tools.Logging;

namespace COMPASS.UnitTests.Infra.Models;

[TestFixture]
public class ProgressTrackerTests
{
    [Test]
    public void Fraction_KnownTotal_ReturnsClampedRatio()
    {
        ProgressTracker progressTracker = new(Quantities.Items())
        {
            Total = 200,
            Progress = 50
        };

        Assert.That(progressTracker.Fraction, Is.EqualTo(0.25));
        Assert.That(progressTracker.Percentage, Is.EqualTo(25));
        Assert.That(progressTracker.IsIndeterminate, Is.False);
        Assert.That(progressTracker.IsComplete, Is.False);
    }

    [Test]
    public void Fraction_UnknownTotal_IsIndeterminate()
    {
        ProgressTracker progressTracker = new(Quantities.Items());

        Assert.That(progressTracker.Fraction, Is.Null);
        Assert.That(progressTracker.IsIndeterminate, Is.True);
        Assert.That(progressTracker.Percentage, Is.EqualTo(0));
        Assert.That(progressTracker.IsComplete, Is.False);
    }

    [Test]
    public void Report_Completed_MarksComplete()
    {
        ProgressTracker progressTracker = new(Quantities.Items())
        {
            Total = 10
        };

        progressTracker.Report(ProgressReports.Completed);

        Assert.That(progressTracker.Progress, Is.EqualTo(10));
        Assert.That(progressTracker.IsComplete, Is.True);
    }

    [Test]
    public void Report_ConcurrentIncrements_NoLostUpdates()
    {
        ProgressTracker progressTracker = new(Quantities.Items())
        {
            Total = 10000
        };

        Parallel.For(0, 10000, _ => progressTracker.Report(ProgressReports.Increment));

        Assert.That(progressTracker.Progress, Is.EqualTo(10000));
        Assert.That(progressTracker.IsComplete, Is.True);
    }

    [Test]
    public async Task LoggerScope_ConcurrentFlows_LogsStayIsolated()
    {
        const int messagesPerFlow = 50;
        ProgressTracker firstTracker = new(Quantities.Items()) { MaxLogEntries = messagesPerFlow };
        ProgressTracker secondTracker = new(Quantities.Items()) { MaxLogEntries = messagesPerFlow };

        Task LogFromScopedFlowAsync(ProgressTracker tracker, string messagePrefix) => Task.Run(async () =>
        {
            using (LoggerScope.Use(tracker))
            {
                for (int messageIndex = 0; messageIndex < messagesPerFlow; messageIndex++)
                {
                    LoggerScope.Current?.Info($"{messagePrefix} {messageIndex}");
                    await Task.Yield();
                }
            }
        });

        await Task.WhenAll(
            LogFromScopedFlowAsync(firstTracker, "First"),
            LogFromScopedFlowAsync(secondTracker, "Second"));

        // Counts are polled (not asserted directly) because posted cross-thread
        // notifications may still be draining when the flows complete.
        Assert.That(() => firstTracker.MessageLog.Count, Is.EqualTo(messagesPerFlow).After(5000, 20));
        Assert.That(() => secondTracker.MessageLog.Count, Is.EqualTo(messagesPerFlow).After(5000, 20));
        Assert.That(firstTracker.MessageLog.All(entry => entry.Msg.StartsWith("First")), Is.True);
        Assert.That(secondTracker.MessageLog.All(entry => entry.Msg.StartsWith("Second")), Is.True);
    }

    [Test]
    public void Report_LogBeyondCap_DropsOldestEntries()
    {
        ProgressTracker progressTracker = new(Quantities.Items())
        {
            MaxLogEntries = 3
        };

        for (int messageIndex = 0; messageIndex < 5; messageIndex++)
        {
            progressTracker.Report(ProgressReports.Log(new LogEntry(Severity.Info, $"Message {messageIndex}")));
        }

        Assert.That(progressTracker.MessageLog, Has.Count.EqualTo(3));
        Assert.That(progressTracker.MessageLog[0].Msg, Is.EqualTo("Message 2"));
        Assert.That(progressTracker.MessageLog[2].Msg, Is.EqualTo("Message 4"));
    }
}
