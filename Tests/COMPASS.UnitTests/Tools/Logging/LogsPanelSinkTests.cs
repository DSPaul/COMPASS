using COMPASS.Common.Tools.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace COMPASS.UnitTests.Tools.Logging
{
    [TestFixture]
    public class LogsPanelSinkTests
    {
        [Test]
        public void Emit_WithoutDispatcher_DoesNotThrow()
        {
            // Arrange: unit tests run without an Avalonia UI thread,
            // so LogsVM.AddLog cannot post to the dispatcher
            var sink = new LogsPanelSink();
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Error,
                null,
                new MessageTemplateParser().Parse("boom"),
                []);

            // Act + Assert: sinks run on caller threads and must never throw,
            // even when the logs panel is unreachable
            Assert.That(() => sink.Emit(logEvent), Throws.Nothing);
        }

        [Test]
        public void Emit_DebugLevel_DoesNotThrow()
        {
            // Arrange
            var sink = new LogsPanelSink();
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Debug,
                null,
                new MessageTemplateParser().Parse("verbose"),
                []);

            // Act + Assert: below-minimum events take the early-return path
            Assert.That(() => sink.Emit(logEvent), Throws.Nothing);
        }
        [Test]
        public void Emit_NullEvent_DoesNotThrow()
        {
            // Arrange
            var sink = new LogsPanelSink();

            // Act + Assert: proves the hardening itself — any failure inside
            // Emit (dead dispatcher, bad event) is swallowed, never propagated
            // to the thread being logged from
            Assert.That(() => sink.Emit(null!), Throws.Nothing);
        }
    }
}
